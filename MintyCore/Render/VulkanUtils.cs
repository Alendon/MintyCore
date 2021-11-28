using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Vulkan;
using VkSemaphore = Silk.NET.Vulkan.Semaphore;

namespace MintyCore.Render
{
    public static unsafe class VulkanUtils
    {
        public static PresentInfoKHR GetPresentInfo(SwapchainKHR* swapchains, uint swapchainCount,
            VkSemaphore* waitSemaphores, uint waitSemaphoreCount, uint* imageIndices, Result* results = null,
            void* pNext = null)
        {
            return new PresentInfoKHR()
            {
                SType = StructureType.PresentInfoKhr,
                PWaitSemaphores = waitSemaphores,
                WaitSemaphoreCount = waitSemaphoreCount,
                PSwapchains = swapchains,
                SwapchainCount = swapchainCount,
                PImageIndices = imageIndices,
                PResults = results,
                PNext = pNext
            };
        }

        public static SubmitInfo GetSubmitInfo(CommandBuffer* commandBuffers, uint commandBufferCount,
            VkSemaphore* waitSemaphores, uint waitSemaphoreCount, VkSemaphore* signalSemaphore,
            uint signalSemaphoreCount, PipelineStageFlags* waitStageMask, void* pNext = null)
        {
            return new SubmitInfo()
            {
                SType = StructureType.SubmitInfo,
                PNext = pNext,
                CommandBufferCount = commandBufferCount,
                PCommandBuffers = commandBuffers,
                PSignalSemaphores = signalSemaphore,
                PWaitSemaphores = waitSemaphores,
                SignalSemaphoreCount = signalSemaphoreCount,
                WaitSemaphoreCount = waitSemaphoreCount,
                PWaitDstStageMask = waitStageMask
            };
        }

        public static string[] EnumerateDeviceExtensions(PhysicalDevice device, byte* layer = null)
        {
            uint extensionCount = 0;
            VulkanEngine._vk.EnumerateDeviceExtensionProperties(device, layer, ref extensionCount, null);
            ExtensionProperties[] properties = new ExtensionProperties[extensionCount];
            VulkanEngine._vk.EnumerateDeviceExtensionProperties(device, layer, ref extensionCount, ref properties[0]);
            string[] extensionNames = new string[extensionCount];
            for (var i = 0; i < extensionCount; i++)
            {
                fixed (byte* name = properties[i].ExtensionName)
                    extensionNames[i] = Marshal.PtrToStringAnsi((IntPtr)name) ?? string.Empty;
            }

            return extensionNames;
        }

        public static string[] EnumerateDeviceLayers(PhysicalDevice device)
        {
            uint layerCount = 0;
            VulkanEngine._vk.EnumerateDeviceLayerProperties(device, ref layerCount, null);
            LayerProperties[] properties = new LayerProperties[layerCount];
            VulkanEngine._vk.EnumerateDeviceLayerProperties(device, ref layerCount, ref properties[0]);
            string[] layerNames = new string[layerCount];
            for (var i = 0; i < layerCount; i++)
            {
                fixed (byte* name = properties[i].LayerName)
                    layerNames[i] = Marshal.PtrToStringAnsi((IntPtr)name) ?? string.Empty;
            }

            return layerNames;
        }

        public static string[] EnumerateInstanceExtensions(byte* layerName = null)
        {
            uint extensionCount = 0;
            VulkanEngine._vk.EnumerateInstanceExtensionProperties(layerName, ref extensionCount, null);
            ExtensionProperties[] properties = new ExtensionProperties[extensionCount];
            VulkanEngine._vk.EnumerateInstanceExtensionProperties(layerName, ref extensionCount, ref properties[0]);
            string[] extensionNames = new string[extensionCount];
            for (var i = 0; i < extensionCount; i++)
            {
                fixed (byte* name = properties[i].ExtensionName)
                    extensionNames[i] = Marshal.PtrToStringAnsi((IntPtr)name) ?? string.Empty;
            }

            return extensionNames;
        }

        public static string[] EnumerateInstanceLayers()
        {
            uint layerCount = 0;
            VulkanEngine._vk.EnumerateInstanceLayerProperties(ref layerCount, null);
            LayerProperties[] properties = new LayerProperties[layerCount];
            VulkanEngine._vk.EnumerateInstanceLayerProperties(ref layerCount, ref properties[0]);
            string[] layerNames = new string[layerCount];
            for (var i = 0; i < layerCount; i++)
            {
                fixed (byte* name = properties[i].LayerName)
                    layerNames[i] = Marshal.PtrToStringAnsi((IntPtr)name) ?? string.Empty;
            }

            return layerNames;
        }

        public static PhysicalDevice[] EnumerateDevices(Instance instance)
        {
            uint deviceCount = 0;
            Assert(VulkanEngine._vk.EnumeratePhysicalDevices(instance, ref deviceCount, null));
            if (deviceCount == 0) return Array.Empty<PhysicalDevice>();

            var devices = new PhysicalDevice[deviceCount];
            Assert(VulkanEngine._vk.EnumeratePhysicalDevices(instance, ref deviceCount, ref devices[0]));
            return devices;
        }

        public static bool FindMemoryType(uint typeFilter, MemoryPropertyFlags requiredFlags, out int memoryTypeIndex)
        {
            PhysicalDeviceMemoryProperties properties;
            VulkanEngine._vk.GetPhysicalDeviceMemoryProperties(VulkanEngine._physicalDevice, out properties);
            for (var i = 0; i < properties.MemoryTypeCount; i++)
            {
                if (((1u << i) & typeFilter) == 0 ||
                    (properties.MemoryTypes[i].PropertyFlags & requiredFlags) != requiredFlags) continue;
                memoryTypeIndex = i;
                return true;
            }

            memoryTypeIndex = -1;
            return false;
        }

        public static void Assert(Result result)
        {
            if (result != Result.Success)
            {
                throw new VulkanException(result);
            }
        }
    }

    class VulkanException : Exception
    {
        public VulkanException(Result result) : base($"A Vulkan Exception occured({result})")
        {
        }

        public VulkanException(string message) : base(message)
        {
        }
    }

    public struct QueueFamilyIndexes
    {
        public uint? GraphicsFamily;
        public uint? PresentFamily;
        public uint? ComputeFamily;
    }
}