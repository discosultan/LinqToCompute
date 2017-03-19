using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using VulkanCore;
using VulkanCore.Ext;

namespace LinqToCompute
{
    public class VulkanDevice : IDisposable
    {
        private bool _disposed;

        public static VulkanDevice Default { get; } = new VulkanDevice(
#if DEBUG
            true
#else
            false
#endif
        );

        public event EventHandler<string> DebugLog;

        private VulkanDevice(bool debug)
        {
            CreateInstance(debug);
            CreateDeviceAndGetQueues();

            if (debug)
                DebugLog += (s, msg) => Debug.WriteLine(msg);
        }

        ~VulkanDevice()
        {
            Dispose();
        }

        public Instance Instance { get; private set; }
        public DebugReportCallbackExt DebugCallback { get; private set; }
        public PhysicalDevice Physical { get; private set; }
        public PhysicalDeviceProperties PhysicalProperties { get; private set; }
        public PhysicalDeviceMemoryProperties PhysicalMemoryProperties { get; private set; }
        public Device Logical { get; private set; }
        public ConcurrentQueue<Queue> Queues { get; } = new ConcurrentQueue<Queue>();
        public int QueueFamilyIndex { get; private set; }

        public void Dispose()
        {
            if (_disposed) return;

            Logical.WaitIdle();

            Logical.Dispose();
            DebugCallback?.Dispose();
            Instance.Dispose();

            GC.SuppressFinalize(this);
            _disposed = true;
        }

        private void CreateInstance(bool debug)
        {
            // Create Vulkan instance.
            const int applicationVersion = 1;
            const string applicationName = "LinqToVulkan";
            var applicationInfo = new ApplicationInfo(applicationName, applicationVersion);

            Instance = new Instance(debug
                ? new InstanceCreateInfo(
                    applicationInfo,
                    new[] { Constant.InstanceLayer.LunarGStandardValidation },
                    new[] { Constant.InstanceExtension.ExtDebugReport })
                : new InstanceCreateInfo(applicationInfo));

            // Attach debug callback if enabled.
            if (debug)
            {
                var debugReportCreateInfo = new DebugReportCallbackCreateInfoExt(
                    DebugReportFlagsExt.All,
                    args =>
                    {
                        DebugLog?.Invoke(this, $"[{args.Flags}][{args.LayerPrefix}] {args.Message}");
                        return args.Flags.HasFlag(DebugReportFlagsExt.Error);
                    }
                );
                DebugCallback = Instance.CreateDebugReportCallbackExt(debugReportCreateInfo);
            }
        }

        private void CreateDeviceAndGetQueues()
        {
            // Get physical device suitable for compute.
            QueueFamilyProperties[] queueFamilyProperties = null;
            foreach (PhysicalDevice physicalDevice in Instance.EnumeratePhysicalDevices())
            {
                queueFamilyProperties = physicalDevice.GetQueueFamilyProperties();
                for (int i = 0; i < queueFamilyProperties.Length; i++)
                {
                    if (queueFamilyProperties[i].QueueFlags.HasFlag(VulkanCore.Queues.Compute))
                    {
                        Physical = physicalDevice;
                        QueueFamilyIndex = i;
                        break;
                    }
                }
            }

            if (Physical == null)
                throw new ApplicationException("No suitable physical device found.");

            PhysicalProperties = Physical.GetProperties();
            PhysicalMemoryProperties = Physical.GetMemoryProperties();
            int numQueues = queueFamilyProperties[QueueFamilyIndex].QueueCount;

            // Create logical device.
            var deviceCreateInfo = new DeviceCreateInfo(
                new[] { new DeviceQueueCreateInfo(QueueFamilyIndex, numQueues, 1.0f) },
                new[] { Constant.DeviceExtension.KhrSwapchain });
            Logical = Physical.CreateDevice(deviceCreateInfo);

            // Get all queues from the queue family.
            for (int i = 0; i < numQueues; i++)
                Queues.Enqueue(Logical.GetQueue(QueueFamilyIndex, i));
        }
    }
}
