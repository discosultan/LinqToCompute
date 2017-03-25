using System;
using System.Runtime.InteropServices;
using LinqToCompute.Utilities;
using VulkanCore;
using Buffer = VulkanCore.Buffer;

namespace LinqToCompute
{
    internal enum ResourceDirection
    {
        CpuToGpu, GpuToCpu
    }

    internal unsafe class VulkanBuffer : IDisposable
    {
        public Type ElementType { get; protected set; }
        public int Count => HostResource.Length;
        public ResourceDirection Direction { get; protected set; }
        public Array HostResource { get; protected set; }
        public int HostStride => ElementType.ManagedSize();
        public long HostSize => Count * HostStride;
        public VulkanDevice Device { get; protected set; }
        public Buffer DeviceResource { get; protected set; }
        public DeviceMemory DeviceMemory { get; protected set; }
        public int DeviceStride => (HostStride < 16 ? HostStride.PowerOfTwo() : HostStride).Align(4);
        public long DeviceAlignedSize { get; protected set; }
        public long DeviceSize => Count * DeviceStride;

        public VulkanBuffer(VulkanDevice device, Array hostBuffer, Type elementType, ResourceDirection direction)
        {
            Device = device;
            Direction = direction;
            HostResource = hostBuffer;
            ElementType = elementType;
        }

        public void Initialize()
        {
            DeviceResource = Device.Logical.CreateBuffer(new BufferCreateInfo(
                DeviceSize,
                BufferUsages.StorageBuffer,
                queueFamilyIndices: new[] { Device.QueueFamilyIndex }));

            MemoryRequirements memReq = DeviceResource.GetMemoryRequirements();
            DeviceAlignedSize = DeviceSize.Align(memReq.Alignment);

            MemoryType[] memoryTypes = Device.PhysicalMemoryProperties.MemoryTypes;
            int memoryTypeIndex;
            if (Direction == ResourceDirection.CpuToGpu)
            {
                memoryTypeIndex = memoryTypes.IndexOf(memReq.MemoryTypeBits, MemoryProperties.DeviceLocal | MemoryProperties.HostVisible);
                if (memoryTypeIndex == -1)
                {
                    memoryTypeIndex = memoryTypes.IndexOf(memReq.MemoryTypeBits, MemoryProperties.HostVisible);
                }
            }
            else
            {
                memoryTypeIndex = memoryTypes.IndexOf(memReq.MemoryTypeBits,
                    MemoryProperties.HostVisible | MemoryProperties.HostCoherent | MemoryProperties.HostCached);
            }

            if (memoryTypeIndex == -1)
                throw new ApplicationException("No suitable memory type found for storage buffer");

            // Some platforms may have a limit on the maximum size of a single allocation.
            // For example, certain systems may fail to create allocations with a size greater than or equal to 4GB.
            // Such a limit is implementation-dependent, and if such a failure occurs then the error
            // VK_ERROR_OUT_OF_DEVICE_MEMORY should be returned.
            try
            {
                DeviceMemory = Device.Logical.AllocateMemory(new MemoryAllocateInfo(memReq.Size, memoryTypeIndex));
            }
            catch (VulkanException ex) when (ex.Result == Result.ErrorOutOfDeviceMemory)
            {
                // TODO: Allocate in chunks instead?
                throw;
            }

            DeviceResource.BindMemory(DeviceMemory);
        }

        public void Write()
        {
            IntPtr dstPtr = DeviceMemory.Map(0, HostSize);
            GCHandle handle = GCHandle.Alloc(HostResource, GCHandleType.Pinned);
            IntPtr srcPtr = handle.AddrOfPinnedObject();

            if (HostStride == DeviceStride)
            {
                System.Buffer.MemoryCopy(
                    srcPtr.ToPointer(),
                    dstPtr.ToPointer(),
                    DeviceAlignedSize,
                    HostSize);
            }
            else
            {
                var srcWalk = (byte*)srcPtr;
                var dstWalk = (byte*)dstPtr;
                for (int i = 0; i < Count; i++)
                {
                    System.Buffer.MemoryCopy(srcWalk, dstWalk, DeviceStride, HostStride);
                    srcWalk += HostStride;
                    dstWalk += DeviceStride;
                }
            }

            handle.Free();
            DeviceMemory.Unmap();
        }

        public void Read()
        {
            IntPtr srcPtr = DeviceMemory.Map(0, HostSize);
            GCHandle handle = GCHandle.Alloc(HostResource, GCHandleType.Pinned);
            IntPtr dstPtr = handle.AddrOfPinnedObject();

            if (HostStride == DeviceStride)
            {
                System.Buffer.MemoryCopy(
                    srcPtr.ToPointer(),
                    dstPtr.ToPointer(),
                    HostSize,
                    HostSize);
            }
            else
            {
                var srcWalk = (byte*)srcPtr;
                var dstWalk = (byte*)dstPtr;
                for (int i = 0; i < Count; i++)
                {
                    System.Buffer.MemoryCopy(srcWalk, dstWalk, HostStride, HostStride);
                    srcWalk += DeviceStride;
                    dstWalk += HostStride;
                }
            }

            handle.Free();
            DeviceMemory.Unmap();
        }

        public void Dispose()
        {
            DeviceResource.Dispose();
            DeviceMemory.Dispose();
        }

        public WriteDescriptorSet GetWriteDescriptorSet(DescriptorSet set, int binding)
        {
            return new WriteDescriptorSet(
                set,
                binding,
                0,
                1,
                DescriptorType.StorageBuffer,
                bufferInfo: new[] { new DescriptorBufferInfo(this) });
        }

        public DescriptorSetLayoutBinding GetDescriptorSetLayoutBinding(int binding)
        {
            return new DescriptorSetLayoutBinding(binding, DescriptorType.StorageBuffer, 1, ShaderStages.Compute);
        }

        public static implicit operator Buffer(VulkanBuffer resource) => resource.DeviceResource;
    }
}
