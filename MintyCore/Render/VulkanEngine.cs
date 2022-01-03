using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using MintyCore.Identifications;
using MintyCore.Utils;
using Silk.NET.Core.Native;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;
using Vulkanizer;
using VkSemaphore = Silk.NET.Vulkan.Semaphore;
using static MintyCore.Render.VulkanUtils;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace MintyCore.Render
{
    /// <summary>
    ///     Base class to interact with the VulkanAPI through the Veldrid Library
    /// </summary>
    public static unsafe class VulkanEngine
    {
#if DEBUG
        internal static bool ValidationLayersActive = true;
#else
        internal static bool ValidationLayersActive = false;
#endif

        internal static string[] DeviceExtensions = new[] { KhrSwapchain.ExtensionName };

        public static readonly Vk Vk = Vk.GetApi();

        public static readonly AllocationCallbacks* AllocationCallback = null;
        public static Instance Instance;
        public static SurfaceKHR Surface;

        public static PhysicalDevice PhysicalDevice;
        public static PhysicalDeviceMemoryProperties PhysicalDeviceMemoryProperties { get; private set; }
        public static Device Device;
        public static QueueFamilyIndexes QueueFamilyIndexes;

        public static Queue GraphicQueue;
        public static Queue PresentQueue;
        public static Queue ComputeQueue;
        public static KhrSurface VkSurface;
        public static KhrSwapchain VkSwapchain;

        public static SwapchainKHR Swapchain;
        public static Image[] SwapchainImages;
        public static Format SwapchainImageFormat;
        public static Extent2D SwapchainExtent;
        public static ImageView[] SwapchainImageViews;
        public static int SwapchainImageCount => SwapchainImages.Length;
        public static Texture DepthTexture;
        public static ImageView DepthImageView;

        public static Framebuffer[] SwapchainFramebuffers;

        public static CommandPool[] GraphicsCommandPool;
        private static CommandPool SingleTimeCommandPool;
        private static Queue<CommandBuffer> SingleTimeCommandBuffers = new();

        public static VkSemaphore SemaphoreImageAvailable;
        public static VkSemaphore SemaphoreRenderingDone;
        public static Fence[] RenderFences;

        internal static void Setup()
        {
            var debug
#if DEBUG
                = true;
#else
			    = false;
#endif
            CreateInstance();
            CreateSurface();
            CreateDevice();
            
            CreateSwapchain();
            CreateSwapchainImageViews();
            CreateCommandPool();

            CreateDepthBuffer();
            RenderPassHandler.CreateMainRenderPass(SwapchainImageFormat);
            CreateFramebuffer();
            
            CreateRenderSemaphore();
            CreateRenderFence();

            Engine.Window!.GetWindow().FramebufferResize += Resized;
        }


        private static CommandBuffer _currentBuffer;
        public static uint _imageIndex;


        public static void PrepareDraw()
        {
            Result acquireResult;

            do
            {
                acquireResult = VkSwapchain.AcquireNextImage(Device, Swapchain, ulong.MaxValue, SemaphoreImageAvailable,
                    default,
                    ref _imageIndex);

                if (acquireResult != Result.Success)
                {
                    RecreateSwapchain();
                }
            } while (acquireResult != Result.Success);

            Assert(Vk.WaitForFences(Device, RenderFences.AsSpan((int)_imageIndex, 1), Vk.True, ulong.MaxValue));
            Assert(Vk.ResetFences(Device, RenderFences.AsSpan((int)_imageIndex, 1)));
            Vk.ResetCommandPool(Device, GraphicsCommandPool[_imageIndex], 0);

            CommandBufferAllocateInfo allocateInfo = new()
            {
                SType = StructureType.CommandBufferAllocateInfo,
                CommandBufferCount = 1,
                CommandPool = GraphicsCommandPool[_imageIndex],
                Level = CommandBufferLevel.Primary
            };
            Assert(Vk.AllocateCommandBuffers(Device, allocateInfo, out _currentBuffer));

            CommandBufferBeginInfo beginInfo = new()
            {
                SType = StructureType.CommandBufferBeginInfo,
                Flags = CommandBufferUsageFlags.CommandBufferUsageOneTimeSubmitBit,
            };
            Assert(Vk.BeginCommandBuffer(_currentBuffer, beginInfo));

            ClearValue* clearValues = stackalloc ClearValue[]
            {
                new()
                {
                    Color =
                    {
                        Float32_0 = 0,
                        Float32_1 = 0,
                        Float32_2 = 0,
                        Float32_3 = 0
                    }
                },
                new()
                {
                    DepthStencil =
                    {
                        Depth = 1
                    }
                }
            };

            RenderPassBeginInfo renderPassBeginInfo = new()
            {
                SType = StructureType.RenderPassBeginInfo,
                Framebuffer = SwapchainFramebuffers[_imageIndex],
                RenderPass = RenderPassHandler.MainRenderPass,
                RenderArea = new Rect2D()
                {
                    Extent = SwapchainExtent,
                    Offset = new(0, 0)
                },
                ClearValueCount = 2,
                PClearValues = clearValues
            };
            Vk.CmdBeginRenderPass(_currentBuffer, renderPassBeginInfo, SubpassContents.SecondaryCommandBuffers);
        }


        public static void ExecuteSecondary(CommandBuffer buffer)
        {
            Vk.CmdExecuteCommands(_currentBuffer, 1, buffer);
        }

        public static void EndDraw()
        {
            Vk.CmdEndRenderPass(_currentBuffer);

            Assert(Vk.EndCommandBuffer(_currentBuffer));

            var imageAvailable = SemaphoreImageAvailable;
            var renderingDone = SemaphoreRenderingDone;

            PipelineStageFlags waitStage = PipelineStageFlags.PipelineStageColorAttachmentOutputBit;

            var buffer = _currentBuffer;
            SubmitInfo submitInfo = new()
            {
                SType = StructureType.SubmitInfo,
                CommandBufferCount = 1,
                PCommandBuffers = &buffer,
                WaitSemaphoreCount = 1,
                SignalSemaphoreCount = 1,
                PWaitSemaphores = &imageAvailable,
                PSignalSemaphores = &renderingDone,
                PWaitDstStageMask = &waitStage
            };

            Assert(Vk.QueueSubmit(GraphicQueue, 1u, submitInfo, RenderFences[_imageIndex]));

            var swapchain = Swapchain;
            var imageindex = _imageIndex;
            PresentInfoKHR presentInfo = new()
            {
                SType = StructureType.PresentInfoKhr,
                WaitSemaphoreCount = 1,
                PWaitSemaphores = &renderingDone,
                PSwapchains = &swapchain,
                SwapchainCount = 1,
                PImageIndices = &imageindex
            };

            VkSwapchain.QueuePresent(PresentQueue, presentInfo);
        }

        private static void CreateRenderFence()
        {
            FenceCreateInfo fenceCreateInfo = new()
            {
                SType = StructureType.FenceCreateInfo,
                Flags = FenceCreateFlags.FenceCreateSignaledBit
            };

            RenderFences = new Fence[SwapchainImageCount];
            for (int i = 0; i < SwapchainImageCount; i++)
            {
                Assert(Vk.CreateFence(Device, fenceCreateInfo, AllocationCallback, out RenderFences[i]));
            }
        }

        private static void CreateRenderSemaphore()
        {
            SemaphoreCreateInfo semaphoreCreateInfo = new()
            {
                SType = StructureType.SemaphoreCreateInfo
            };

            Assert(Vk.CreateSemaphore(Device, semaphoreCreateInfo, AllocationCallback,
                out SemaphoreImageAvailable));
            Assert(Vk.CreateSemaphore(Device, semaphoreCreateInfo, AllocationCallback,
                out SemaphoreRenderingDone));
        }

        private static void CreateCommandPool()
        {
            GraphicsCommandPool = new CommandPool[SwapchainImageCount];
            CommandPoolCreateInfo createInfo = new()
            {
                SType = StructureType.CommandPoolCreateInfo,
                QueueFamilyIndex = QueueFamilyIndexes.GraphicsFamily!.Value
            };

            for (int i = 0; i < SwapchainImageCount; i++)
            {
                Assert(Vk.CreateCommandPool(Device, createInfo, AllocationCallback, out GraphicsCommandPool[i]));
            }

            createInfo.Flags = CommandPoolCreateFlags.CommandPoolCreateResetCommandBufferBit;
            Vk.CreateCommandPool(Device, createInfo, AllocationCallback, out SingleTimeCommandPool);
        }

        private static void CreateFramebuffer()
        {
            SwapchainFramebuffers = new Framebuffer[SwapchainImageCount];
            ImageView* imageViews = stackalloc ImageView[2];

            imageViews[1] = DepthImageView;

            for (int i = 0; i < SwapchainImageCount; i++)
            {
                imageViews[0] = SwapchainImageViews[i];
                FramebufferCreateInfo createInfo = new()
                {
                    SType = StructureType.FramebufferCreateInfo,
                    AttachmentCount = 2,
                    PAttachments = imageViews,
                    Height = SwapchainExtent.Height,
                    Width = SwapchainExtent.Width,
                    RenderPass = RenderPassHandler.MainRenderPass,
                    Layers = 1
                };

                Assert(Vk.CreateFramebuffer(Device, createInfo, AllocationCallback,
                    out SwapchainFramebuffers[i]));
            }
        }

        private static void CreateSwapchainImageViews()
        {
            SwapchainImageViews = new ImageView[SwapchainImageCount];

            for (int i = 0; i < SwapchainImageCount; i++)
            {
                ImageViewCreateInfo createInfo = new()
                {
                    SType = StructureType.ImageViewCreateInfo,
                    Image = SwapchainImages[i],
                    ViewType = ImageViewType.ImageViewType2D,
                    Format = SwapchainImageFormat,
                    Components =
                    {
                        A = ComponentSwizzle.Identity,
                        B = ComponentSwizzle.Identity,
                        G = ComponentSwizzle.Identity,
                        R = ComponentSwizzle.Identity
                    },
                    SubresourceRange =
                    {
                        AspectMask = ImageAspectFlags.ImageAspectColorBit,
                        LayerCount = 1,
                        LevelCount = 1,
                        BaseArrayLayer = 0,
                        BaseMipLevel = 0
                    }
                };

                Assert(Vk.CreateImageView(Device, createInfo, AllocationCallback, out SwapchainImageViews[i]));
            }
        }

        private static void CreateSwapchain()
        {
            Logger.WriteLog("Creating swapchain", LogImportance.DEBUG, "Render");

            var (capabilities, formats, presentModes) = GetSwapChainSupport();

            var format = formats.FirstOrDefault(x => x.Format == Format.B8G8R8A8Unorm, formats[0]);

            var presentMode = presentModes.Contains(PresentModeKHR.PresentModeMailboxKhr)
                ? PresentModeKHR.PresentModeMailboxKhr
                : presentModes[0];

            Extent2D extent = GetSwapChainExtent(capabilities);

            var imageCount = capabilities.MinImageCount + 1;
            if (capabilities.MaxImageCount > 0 &&
                imageCount > capabilities.MaxImageCount)
            {
                imageCount = capabilities.MaxImageCount;
            }

            SwapchainCreateInfoKHR createInfo = new()
            {
                SType = StructureType.SwapchainCreateInfoKhr,
                PresentMode = presentMode,
                ImageExtent = extent,
                ImageFormat = format.Format,
                ImageColorSpace = format.ColorSpace,
                ImageArrayLayers = 1,
                ImageUsage = ImageUsageFlags.ImageUsageColorAttachmentBit,
                Surface = Surface,
                PreTransform = capabilities.CurrentTransform,
                CompositeAlpha = CompositeAlphaFlagsKHR.CompositeAlphaOpaqueBitKhr,
                Clipped = Vk.True,
                OldSwapchain = default,
                MinImageCount = imageCount
            };

            var indices = QueueFamilyIndexes;
            uint* queueFamilyIndices = stackalloc uint[2]
                { QueueFamilyIndexes.GraphicsFamily!.Value, QueueFamilyIndexes.PresentFamily!.Value };

            if (indices.GraphicsFamily!.Value != indices.PresentFamily!.Value)
            {
                createInfo.ImageSharingMode = SharingMode.Concurrent;
                createInfo.QueueFamilyIndexCount = 2;
                createInfo.PQueueFamilyIndices = queueFamilyIndices;
            }
            else
            {
                createInfo.ImageSharingMode = SharingMode.Exclusive;
            }

            if (!Vk.TryGetDeviceExtension(Instance, Device, out VkSwapchain))
            {
                Logger.WriteLog("KhrSwapchain extension not found", LogImportance.EXCEPTION, "Render");
            }

            Assert(VkSwapchain.CreateSwapchain(Device, createInfo, AllocationCallback, out Swapchain));

            VkSwapchain.GetSwapchainImages(Device, Swapchain, ref imageCount, null);
            SwapchainImages = new Image[imageCount];
            VkSwapchain.GetSwapchainImages(Device, Swapchain, ref imageCount, out SwapchainImages[0]);

            SwapchainImageFormat = format.Format;
            SwapchainExtent = extent;
        }

        private static void RecreateSwapchain()
        {
            Vector2D<int> framebufferSize = Engine.Window.GetWindow().FramebufferSize;

            while (framebufferSize.X == 0 || framebufferSize.Y == 0)
            {
                framebufferSize = Engine.Window.GetWindow().FramebufferSize;
                Engine.Window.GetWindow().DoEvents();
            }

            Vk.DeviceWaitIdle(Device);
            CleanupSwapchain();

            CreateSwapchain();
            CreateSwapchainImageViews();
            CreateDepthBuffer();
            RenderPassHandler.CreateMainRenderPass(SwapchainImageFormat);
            CreateFramebuffer();
        }

        private static void CreateDepthBuffer()
        {
            Extent3D extent = new Extent3D(SwapchainExtent.Width, SwapchainExtent.Height, 1);

            uint[] queues = { QueueFamilyIndexes.GraphicsFamily!.Value };
            TextureDescription description = TextureDescription.Texture2D(SwapchainExtent.Width, SwapchainExtent.Height,
                1, 1, Format.D32Sfloat, TextureUsage.DEPTH_STENCIL);
            DepthTexture = new Texture(ref description);

            ImageViewCreateInfo createInfo = new()
            {
                SType = StructureType.ImageViewCreateInfo,
                Image = DepthTexture.Image,
                ViewType = ImageViewType.ImageViewType2D,
                Format = DepthTexture.Format,
                SubresourceRange =
                {
                    AspectMask = ImageAspectFlags.ImageAspectDepthBit,
                    LayerCount = 1,
                    LevelCount = 1,
                    BaseArrayLayer = 0,
                    BaseMipLevel = 0
                },
            };

            Assert(Vk.CreateImageView(Device, createInfo, AllocationCallback, out DepthImageView));
        }

        private static Extent2D GetSwapChainExtent(SurfaceCapabilitiesKHR swapchainSupportCapabilities)
        {
            if (swapchainSupportCapabilities.CurrentExtent.Width != uint.MaxValue)
            {
                return swapchainSupportCapabilities.CurrentExtent;
            }

            var actualExtent = new Extent2D
            {
                Height = (uint)Engine.Window.GetWindow().FramebufferSize.Y,
                Width = (uint)Engine.Window.GetWindow().FramebufferSize.X
            };
            actualExtent.Width = new[]
            {
                swapchainSupportCapabilities.MinImageExtent.Width,
                new[] { swapchainSupportCapabilities.MaxImageExtent.Width, actualExtent.Width }.Min()
            }.Max();
            actualExtent.Height = new[]
            {
                swapchainSupportCapabilities.MinImageExtent.Height,
                new[] { swapchainSupportCapabilities.MaxImageExtent.Height, actualExtent.Height }.Min()
            }.Max();

            return actualExtent;
        }

        private static (SurfaceCapabilitiesKHR, SurfaceFormatKHR[], PresentModeKHR[]) GetSwapChainSupport()
        {
            VkSurface.GetPhysicalDeviceSurfaceCapabilities(PhysicalDevice, Surface, out var capabilities);

            uint surfaceFormatCount = 0;
            VkSurface.GetPhysicalDeviceSurfaceFormats(PhysicalDevice, Surface, ref surfaceFormatCount, null);
            SurfaceFormatKHR[] surfaceFormats = new SurfaceFormatKHR[surfaceFormatCount];
            VkSurface.GetPhysicalDeviceSurfaceFormats(PhysicalDevice, Surface, ref surfaceFormatCount,
                out surfaceFormats[0]);

            uint surfaceModeCount = 0;
            VkSurface.GetPhysicalDeviceSurfacePresentModes(PhysicalDevice, Surface, ref surfaceModeCount, null);
            PresentModeKHR[] presentModes = new PresentModeKHR[surfaceModeCount];
            VkSurface.GetPhysicalDeviceSurfacePresentModes(PhysicalDevice, Surface, ref surfaceModeCount,
                out presentModes[0]);

            return (capabilities, surfaceFormats, presentModes);
        }

        private static void CreateDevice()
        {
            Logger.WriteLog($"Creating device", LogImportance.DEBUG, "Render");
            PhysicalDevice = ChoosePhysicalDevice(EnumerateDevices(Instance));


            Vk.GetPhysicalDeviceMemoryProperties(PhysicalDevice, out var memoryProperties);
            PhysicalDeviceMemoryProperties = memoryProperties;

            QueueFamilyIndexes = GetQueueFamilyIndexes(PhysicalDevice);

            uint queueCount = QueueFamilyIndexes.GraphicsFamily!.Value != QueueFamilyIndexes.ComputeFamily!.Value
                ? 2u
                : 1u;
            if (QueueFamilyIndexes.GraphicsFamily!.Value != QueueFamilyIndexes.PresentFamily!.Value) queueCount++;
            DeviceQueueCreateInfo* queueCreateInfo = stackalloc DeviceQueueCreateInfo[(int)queueCount];
            float priority = 1f;

            queueCreateInfo[0] = new DeviceQueueCreateInfo()
            {
                SType = StructureType.DeviceQueueCreateInfo,
                QueueCount = 1,
                QueueFamilyIndex = QueueFamilyIndexes.GraphicsFamily!.Value,
                PQueuePriorities = &priority
            };
            queueCreateInfo[QueueFamilyIndexes.GraphicsFamily == QueueFamilyIndexes.ComputeFamily ? 0 : 1] = new()
            {
                SType = StructureType.DeviceQueueCreateInfo,
                QueueCount = 1,
                QueueFamilyIndex = QueueFamilyIndexes.ComputeFamily!.Value,
                PQueuePriorities = &priority
            };
            queueCreateInfo
                [QueueFamilyIndexes.GraphicsFamily == QueueFamilyIndexes.PresentFamily ? 0 : queueCount - 1] = new()
            {
                SType = StructureType.DeviceQueueCreateInfo,
                QueueCount = 1,
                QueueFamilyIndex = QueueFamilyIndexes.PresentFamily!.Value,
                PQueuePriorities = &priority
            };

            PhysicalDeviceFeatures enabledFeatures = new()
            {
                SamplerAnisotropy = Vk.True
            };
            
            DeviceCreateInfo deviceCreateInfo = new()
            {
                SType = StructureType.DeviceCreateInfo,
                PQueueCreateInfos = queueCreateInfo,
                QueueCreateInfoCount = queueCount,
                PEnabledFeatures = &enabledFeatures
            };

            var extensions = new HashSet<string>(EnumerateDeviceExtensions(PhysicalDevice));
            foreach (var extension in DeviceExtensions)
            {
                if (!extensions.Contains(extension))
                {
                    throw new MintyCoreException($"Missing device extension {extension}");
                }
            }

            deviceCreateInfo.EnabledExtensionCount = (uint)DeviceExtensions.Length;
            deviceCreateInfo.PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr(DeviceExtensions);

            Assert(Vk.CreateDevice(PhysicalDevice, deviceCreateInfo, AllocationCallback, out Device));
            SilkMarshal.Free((nint)deviceCreateInfo.PpEnabledExtensionNames);

            Vk.GetDeviceQueue(Device, QueueFamilyIndexes.GraphicsFamily.Value, 0, out GraphicQueue);
            Vk.GetDeviceQueue(Device, QueueFamilyIndexes.ComputeFamily.Value, 0, out ComputeQueue);
            Vk.GetDeviceQueue(Device, QueueFamilyIndexes.PresentFamily.Value, 0, out PresentQueue);
        }

        private static QueueFamilyIndexes GetQueueFamilyIndexes(PhysicalDevice device)
        {
            QueueFamilyIndexes indexes = default;

            uint queueFamilyCount = 0;
            Vk.GetPhysicalDeviceQueueFamilyProperties(device, &queueFamilyCount, null);
            QueueFamilyProperties[] queueFamilyProperties = new QueueFamilyProperties[queueFamilyCount];
            Vk.GetPhysicalDeviceQueueFamilyProperties(device, &queueFamilyCount, out queueFamilyProperties[0]);

            for (var i = 0u;
                i < queueFamilyCount && (!indexes.GraphicsFamily.HasValue && !indexes.ComputeFamily.HasValue &&
                                         !indexes.PresentFamily.HasValue);
                i++)
            {
                var queueFamily = queueFamilyProperties[i];

                if (queueFamily.QueueFlags.HasFlag(QueueFlags.QueueGraphicsBit) && !indexes.GraphicsFamily.HasValue)
                {
                    indexes.GraphicsFamily = i;
                }

                if (queueFamily.QueueFlags.HasFlag(QueueFlags.QueueComputeBit) && !indexes.ComputeFamily.HasValue)
                {
                    indexes.ComputeFamily = i;
                }

                VkSurface.GetPhysicalDeviceSurfaceSupport(device, i, Surface, out var presentSupport);

                if (presentSupport == Vk.True && !indexes.PresentFamily.HasValue)
                {
                    indexes.PresentFamily = i;
                }
            }


            return indexes;
        }

        private static PhysicalDevice ChoosePhysicalDevice(PhysicalDevice[] devices)
        {
            if (devices.Length == 0)
                throw new VulkanException("No graphic device found");

            PhysicalDeviceProperties[] deviceProperties = new PhysicalDeviceProperties[devices.Length];

            for (int i = 0; i < devices.Length; i++)
            {
                Vk.GetPhysicalDeviceProperties(devices[i], out deviceProperties[i]);
            }

            for (int i = 0; i < devices.Length; i++)
            {
                if (deviceProperties[i].DeviceType == PhysicalDeviceType.DiscreteGpu)
                {
                    return devices[i];
                }
            }

            for (int i = 0; i < devices.Length; i++)
            {
                if (deviceProperties[i].DeviceType == PhysicalDeviceType.VirtualGpu)
                {
                    return devices[i];
                }
            }

            for (int i = 0; i < devices.Length; i++)
            {
                if (deviceProperties[i].DeviceType == PhysicalDeviceType.IntegratedGpu)
                {
                    return devices[i];
                }
            }


            return devices[0];
        }

        private static void CreateSurface()
        {
            Logger.WriteLog("Creating surface", LogImportance.DEBUG, "Render");
            if (!Vk.TryGetInstanceExtension(Instance, out VkSurface))
            {
                throw new MintyCoreException("KHR_surface extension not found.");
            }

            Surface = Engine.Window!.GetWindow().VkSurface!.Create(Instance.ToHandle(), AllocationCallback)
                .ToSurface();
        }

        private static string[]? GetValidationLayers()
        {
            string[][] validationLayerNamesPriorityList =
            {
                new[] { "VK_LAYER_KHRONOS_validation" },
                new[] { "VK_LAYER_LUNARG_standard_validation" },
                new[]
                {
                    "VK_LAYER_GOOGLE_threading",
                    "VK_LAYER_LUNARG_parameter_validation",
                    "VK_LAYER_LUNARG_object_tracker",
                    "VK_LAYER_LUNARG_core_validation",
                    "VK_LAYER_GOOGLE_unique_objects",
                }
            };

            var availableLayersName = EnumerateInstanceLayers();

            string[]? value = null;

            foreach (var validationSet in validationLayerNamesPriorityList)
            {
                if (validationSet.All(validationName => availableLayersName.Contains(validationName)))
                {
                    value = validationSet;
                    break;
                }
            }

            return value;
        }

        private static void CreateInstance()
        {
            Logger.WriteLog("Creating instance", LogImportance.DEBUG, "Render");
            ApplicationInfo applicationInfo = new()
            {
                SType = StructureType.ApplicationInfo
            };

            InstanceCreateInfo createInfo = new()
            {
                SType = StructureType.InstanceCreateInfo,
                PApplicationInfo = &applicationInfo
            };

            var validationLayers = GetValidationLayers();

            if (validationLayers == null)
            {
                ValidationLayersActive = false;
            }

            if (ValidationLayersActive)
            {
                createInfo.EnabledLayerCount = (uint)validationLayers.Length;
                createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr(validationLayers);
            }

            var extensions = new HashSet<string>(EnumerateInstanceExtensions());
            var windowExtensionPtr =
                Engine.Window!.GetWindow().VkSurface!.GetRequiredExtensions(out var windowExtensionCount);
            var windowExtensions = SilkMarshal.PtrToStringArray((nint)windowExtensionPtr, (int)windowExtensionCount);

            foreach (var extension in windowExtensions)
            {
                if (!extensions.Contains(extension))
                {
                    throw new MintyCoreException(
                        $"The following vulkan extension {extension} is required but not available");
                }
            }

            createInfo.EnabledExtensionCount = windowExtensionCount;
            createInfo.PpEnabledExtensionNames = windowExtensionPtr;

            Assert(Vk.CreateInstance(createInfo, AllocationCallback, out Instance));
            Vk.CurrentInstance = Instance;

            SilkMarshal.Free((nint)createInfo.PpEnabledLayerNames);
        }

        private static void Resized(Vector2D<int> obj)
        {
        }

        internal static void CleanupSwapchain()
        {
            foreach (var framebuffer in SwapchainFramebuffers)
            {
                Vk.DestroyFramebuffer(Device, framebuffer, AllocationCallback);
            }

            RenderPassHandler.DestroyMainRenderPass();
            foreach (var imageView in SwapchainImageViews)
            {
                Vk.DestroyImageView(Device, imageView, AllocationCallback);
            }

            Vk.DestroyImageView(Device, DepthImageView, AllocationCallback);
            DepthTexture.Dispose();

            VkSwapchain.DestroySwapchain(Device, Swapchain, AllocationCallback);
        }

        internal static void Shutdown()
        {
            Logger.WriteLog("Shutdown Vulkan", LogImportance.INFO, "Render");
            Vk.DeviceWaitIdle(Device);

            foreach (var fence in RenderFences)
            {
                Vk.DestroyFence(Device, fence, AllocationCallback);
            }

            Vk.DestroySemaphore(Device, SemaphoreImageAvailable, AllocationCallback);
            Vk.DestroySemaphore(Device, SemaphoreRenderingDone, AllocationCallback);

            Vk.DestroyCommandPool(Device, SingleTimeCommandPool, AllocationCallback);
            foreach (var commandPool in GraphicsCommandPool)
            {
                Vk.DestroyCommandPool(Device, commandPool, AllocationCallback);
            }

            CleanupSwapchain();
            Vk.DestroyDevice(Device, AllocationCallback);
            VkSurface.DestroySurface(Instance, Surface, AllocationCallback);
            Vk.DestroyInstance(Instance, AllocationCallback);
        }

        public static void WaitForAll()
        {
            Assert(Vk.DeviceWaitIdle(Device));
        }

        public static CommandBuffer GetSingleTimeCommandBuffer()
        {
            if (!SingleTimeCommandBuffers.TryDequeue(out var buffer))
            {
                CommandBufferAllocateInfo allocateInfo = new()
                {
                    SType = StructureType.CommandBufferAllocateInfo,
                    Level = CommandBufferLevel.Primary,
                    CommandBufferCount = 1,
                    CommandPool = SingleTimeCommandPool
                };
                Assert(Vk.AllocateCommandBuffers(Device, allocateInfo, out buffer));
            }

            Assert(Vk.BeginCommandBuffer(buffer,
                new CommandBufferBeginInfo
                {
                    SType = StructureType.CommandBufferBeginInfo,
                    Flags = CommandBufferUsageFlags.CommandBufferUsageOneTimeSubmitBit
                }));
            return buffer;
        }

        public static void ExecuteSingleTimeCommandBuffer(CommandBuffer buffer)
        {
            FenceCreateInfo fenceCreateInfo = new()
            {
                SType = StructureType.FenceCreateInfo
            };
            Assert(Vk.CreateFence(Device, in fenceCreateInfo, AllocationCallback, out var fence));
            
            Vk.EndCommandBuffer(buffer);

            SubmitInfo submitInfo = new()
            {
                SType = StructureType.SubmitInfo,
                CommandBufferCount = 1,
                PCommandBuffers = &buffer,
            };
            Assert(Vk.QueueSubmit(GraphicQueue, 1, submitInfo, fence));
            Vk.WaitForFences(Device, 1, in fence, Vk.True, ulong.MaxValue);
            Vk.ResetCommandBuffer(buffer, 0);
            Vk.DestroyFence(Device, fence, AllocationCallback);
            SingleTimeCommandBuffers.Enqueue(buffer);
        }

        public static void ClearColorTexture(Texture texture, ClearColorValue clearColorValue)
        {
            uint layers = texture.ArrayLayers;
            if ((texture.Usage & TextureUsage.CUBEMAP) != 0)
            {
                layers *= 6;
            }

            ImageSubresourceRange subresourceRange = new()
            {
                AspectMask = ImageAspectFlags.ImageAspectColorBit,
                LayerCount = layers,
                LevelCount = texture.MipLevels,
                BaseArrayLayer = 0,
                BaseMipLevel = 0
            };

            var buffer = GetSingleTimeCommandBuffer();
            texture.TransitionImageLayout(buffer, 0, texture.MipLevels, 0, layers, ImageLayout.TransferDstOptimal);
            Vk.CmdClearColorImage(buffer, texture.Image, ImageLayout.TransferDstOptimal, clearColorValue, 1,
                subresourceRange);
            texture.TransitionImageLayout(buffer, 0, texture.MipLevels, 0, layers,
                texture.IsSwapchainTexture ? ImageLayout.PresentSrcKhr : ImageLayout.ColorAttachmentOptimal);
            ExecuteSingleTimeCommandBuffer(buffer);
        }

        public static void ClearDepthTexture(Texture texture, ClearDepthStencilValue clearDepthStencilValue)
        {
            uint effectiveLayers = texture.ArrayLayers;
            if ((texture.Usage & TextureUsage.CUBEMAP) != 0)
            {
                effectiveLayers *= 6;
            }

            ImageAspectFlags aspect = FormatHelpers.IsStencilFormat(texture.Format)
                ? ImageAspectFlags.ImageAspectDepthBit | ImageAspectFlags.ImageAspectStencilBit
                : ImageAspectFlags.ImageAspectDepthBit;

            ImageSubresourceRange range = new(
                aspect,
                0,
                texture.MipLevels,
                0,
                effectiveLayers);

            var cb = GetSingleTimeCommandBuffer();

            texture.TransitionImageLayout(cb, 0, texture.MipLevels, 0, effectiveLayers, ImageLayout.TransferDstOptimal);
            Vk.CmdClearDepthStencilImage(
                cb,
                texture.Image,
                ImageLayout.TransferDstOptimal,
                clearDepthStencilValue,
                1,
                range);
            texture.TransitionImageLayout(cb, 0, texture.MipLevels, 0, effectiveLayers,
                ImageLayout.DepthStencilAttachmentOptimal);
            ExecuteSingleTimeCommandBuffer(cb);
        }

        public static void TransitionImageLayout(Texture texture, ImageLayout layout)
        {
            var cb = GetSingleTimeCommandBuffer();
            texture.TransitionImageLayout(cb, 0, texture.MipLevels, 0, texture.ArrayLayers, layout);
            ExecuteSingleTimeCommandBuffer(cb);
        }
    }
}