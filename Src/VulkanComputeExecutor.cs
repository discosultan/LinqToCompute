using System;
using System.Collections.Generic;
using VulkanCore;

namespace LinqToCompute
{
    internal class VulkanComputeExecutor : IDisposable
    {
        private bool _disposed;

        private Fence _fence;
        private CommandPool _commandPool;
        private CommandBuffer _commandBuffer;
        private DescriptorSetLayout _descriptorSetLayout;
        private PipelineLayout _pipelineLayout;
        private Pipeline _pipeline;
        private DescriptorPool _descriptorPool;
        private DescriptorSet _descriptorSet;

        public VulkanComputeExecutor(VulkanDevice device)
        {
            Device = device;
        }

        public VulkanDevice Device { get; }

        public List<VulkanBuffer> Inputs { get; } = new List<VulkanBuffer>();
        public VulkanBuffer Output { get; set; }
        public byte[] SpirV { get; set; }

        public void GpuSetup()
        {
            Inputs.ForEach(input => input.Initialize());
            Output.Initialize();
            CreateFence();
            CreateDescriptors();
            CreateComputePipeline();
            CreateCommandBuffer();
            RecordDispatchCommand();
        }

        public void GpuTransferInput()
        {
            Inputs.ForEach(input => input.Write());
        }

        public void GpuExecute()
        {
            Queue queue;
            while (!Device.Queues.TryDequeue(out queue)) { }
            queue.Submit(new SubmitInfo(commandBuffers: new[] { _commandBuffer }), _fence);
            _fence.Wait();
            Device.Queues.Enqueue(queue);
        }

        public void GpuTransferOutput()
        {
            Output.Read();
        }

        public void Dispose()
        {
            if (_disposed) return;

            Output.Dispose();
            Inputs.ForEach(input => input.Dispose());
            _descriptorPool.Dispose();
            _pipeline.Dispose();
            _pipelineLayout.Dispose();
            _descriptorSetLayout.Dispose();
            _commandPool.Dispose();
            _fence.Dispose();

            _disposed = true;
        }

        private void CreateFence()
        {
            _fence = Device.Logical.CreateFence();
        }

        private void CreateCommandBuffer()
        {
            _commandPool = Device.Logical.CreateCommandPool(new CommandPoolCreateInfo(Device.QueueFamilyIndex));
            _commandBuffer = _commandPool.AllocateBuffers(new CommandBufferAllocateInfo(CommandBufferLevel.Primary, 1))[0];
        }

        private void CreateComputePipeline()
        {
            var pipelineLayoutCreateInfo = new PipelineLayoutCreateInfo(new[] { _descriptorSetLayout });
            _pipelineLayout = Device.Logical.CreatePipelineLayout(pipelineLayoutCreateInfo);

            using (ShaderModule shader = Device.Logical.CreateShaderModule(new ShaderModuleCreateInfo(SpirV)))
            {
                var computePipelineCreateInfo = new ComputePipelineCreateInfo(
                    new PipelineShaderStageCreateInfo(ShaderStages.Compute, shader, "main"),
                    _pipelineLayout);
                _pipeline = Device.Logical.CreateComputePipeline(computePipelineCreateInfo);
            }
        }

        private void CreateDescriptors()
        {
            int bindingCount = Inputs.Count + 1; // + 1 output.

            // Setup bindings.
            var bindings = new DescriptorSetLayoutBinding[bindingCount];
            for (int i = 0; i < Inputs.Count; i++)
                bindings[i] = Inputs[i].GetDescriptorSetLayoutBinding(i);
            bindings[Inputs.Count] = Output.GetDescriptorSetLayoutBinding(Inputs.Count);

            _descriptorSetLayout = Device.Logical.CreateDescriptorSetLayout(new DescriptorSetLayoutCreateInfo(bindings));
            var descriptorPoolCreateInfo = new DescriptorPoolCreateInfo(
                1,
                new[] { new DescriptorPoolSize(DescriptorType.StorageBuffer, bindingCount) });
            _descriptorPool = Device.Logical.CreateDescriptorPool(descriptorPoolCreateInfo);
            _descriptorSet = _descriptorPool.AllocateSets(new DescriptorSetAllocateInfo(1, _descriptorSetLayout))[0];

            // Setup write descriptors.
            var writeDescriptorSets = new WriteDescriptorSet[bindingCount];
            for (int i = 0; i < Inputs.Count; i++)
                writeDescriptorSets[i] = Inputs[i].GetWriteDescriptorSet(_descriptorSet, i);
            writeDescriptorSets[Inputs.Count] = Output.GetWriteDescriptorSet(_descriptorSet, Inputs.Count);

            _descriptorPool.UpdateSets(writeDescriptorSets);
        }

        private void RecordDispatchCommand()
        {
            int computeWorkgroupXCount = (int)Math.Ceiling(Inputs[0].HostResource.Length / 256.0);

            _commandBuffer.Begin(new CommandBufferBeginInfo(CommandBufferUsages.OneTimeSubmit));
            _commandBuffer.CmdBindPipeline(PipelineBindPoint.Compute, _pipeline);
            _commandBuffer.CmdBindDescriptorSet(PipelineBindPoint.Compute, _pipelineLayout, _descriptorSet);
            _commandBuffer.CmdDispatch(computeWorkgroupXCount, 1, 1);
            _commandBuffer.End();
        }
    }
}