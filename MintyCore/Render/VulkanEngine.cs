using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using MintyCore.Utils;
using Silk.NET.Core.Native;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;
using Vulkanizer;
using VkSemaphore = Silk.NET.Vulkan.Semaphore;
using static MintyCore.Render.VulkanUtils;

namespace MintyCore.Render
{
    /// <summary>
    ///     Base class to interact with the VulkanAPI through the Veldrid Library
    /// </summary>
    public static unsafe class VulkanEngine
    {
#if DEBUG
        internal static bool _validationLayersActive = true;
#else
        internal static bool _validationLayersActive = false;
#endif

        internal static string[] _deviceExtensions = new[] { KhrSwapchain.ExtensionName };

        public static readonly Vk _vk = Vk.GetApi();

        public static readonly AllocationCallbacks* _allocationCallback = null;
        public static Instance _instance;
        public static SurfaceKHR _surface;
        public static IWindow _window;

        public static PhysicalDevice _physicalDevice;
        public static Device _device;
        public static QueueFamilyIndexes _queueFamilyIndexes;

        public static Queue _graphicQueue;
        public static Queue _presentQueue;
        public static Queue _computeQueue;
        public static KhrSurface _vkSurface;
        public static KhrSwapchain _vkSwapchain;

        public static SwapchainKHR _swapchain;
        public static Image[] _swapchainImages;
        public static Format _swapchainImageFormat;
        public static Extent2D _swapchainExtent;
        public static ImageView[] _swapchainImageViews;
        public static int SwapchainImageCount => _swapchainImages.Length;

        public static RenderPass MainRenderPass;
        public static Framebuffer[] _swapchainFramebuffers;

        public static Shader _triangleFragShader;
        public static Shader _triangleVertShader;
        public static PipelineLayout _pipelineLayout;
        public static Pipeline _pipeline;

        public static CommandPool[] GraphicsCommandPool;

        public static VkSemaphore _semaphoreImageAvailable;
        public static VkSemaphore _semaphoreRenderingDone;
        public static Fence[] _renderFences;

        public static Mesh triangleMesh;

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
            CreateImageViews();
            CreateMainRenderPass();
            CreateFramebuffer();
            CreateSimplePipeline();
            CreateCommandPool();
            CreateSyncStructures();
            CreateMesh();

            Engine.Window!.GetWindow().FramebufferResize += Resized;
        }

        private static void CreateMesh()
        {
            triangleMesh = MeshHandler.CreateMesh(new DefaultVertex[]
            {
                new(new Vector3(0, -0.5f, 0), new Vector3(1, 1, 1), Vector3.Zero, Vector2.One),
                new(new Vector3(0.5f, 0.5f, 0), new Vector3(0, 0, 0), Vector3.One, Vector2.One),
                new(new Vector3(-0.5f, 0.5f, 0), new Vector3(0.5f, 0.5f, 0.5f), Vector3.One, Vector2.One)
            });
        }

        private static CommandBuffer currentBuffer;
        private static uint imageIndex;

        public static void PrepareDraw()
        {
            _vkSwapchain.AcquireNextImage(_device, _swapchain, ulong.MaxValue, _semaphoreImageAvailable, default,
                ref imageIndex);

            Assert(_vk.WaitForFences(_device, _renderFences.AsSpan((int)imageIndex, 1), Vk.True, ulong.MaxValue));
            Assert(_vk.ResetFences(_device, _renderFences.AsSpan((int)imageIndex, 1)));
            _vk.ResetCommandPool(_device, GraphicsCommandPool[imageIndex], 0);

            CommandBufferAllocateInfo allocateInfo = new()
            {
                SType = StructureType.CommandBufferAllocateInfo,
                CommandBufferCount = 1,
                CommandPool = GraphicsCommandPool[imageIndex],
                Level = CommandBufferLevel.Primary
            };
            Assert(_vk.AllocateCommandBuffers(_device, allocateInfo, out currentBuffer));

            CommandBufferBeginInfo beginInfo = new()
            {
                SType = StructureType.CommandBufferBeginInfo,
                Flags = CommandBufferUsageFlags.CommandBufferUsageOneTimeSubmitBit,
            };
            Assert(_vk.BeginCommandBuffer(currentBuffer, beginInfo));
        }

        public static void Draw()
        {
            ClearValue clearValue = new()
            {
                Color = new ClearColorValue()
                {
                    Float32_0 = 0,
                    Float32_1 = 0,
                    Float32_2 = 0,
                    Float32_3 = 0
                }
            };
            RenderPassBeginInfo renderPassBeginInfo = new()
            {
                SType = StructureType.RenderPassBeginInfo,
                Framebuffer = _swapchainFramebuffers[imageIndex],
                RenderPass = MainRenderPass,
                RenderArea = new Rect2D()
                {
                    Extent = _swapchainExtent,
                    Offset = new(0, 0)
                },
                ClearValueCount = 1,
                PClearValues = &clearValue
            };
            _vk.CmdBeginRenderPass(currentBuffer, renderPassBeginInfo, SubpassContents.Inline);

            _vk.CmdBindPipeline(currentBuffer, PipelineBindPoint.Graphics, _pipeline);

            var vertBuffer = triangleMesh.Buffer;
            var offsets = 0ul;
            _vk.CmdBindVertexBuffers(currentBuffer, 0, 1, &vertBuffer, offsets);

            _vk.CmdDraw(currentBuffer, 3, 1, 0, 0);

            _vk.CmdEndRenderPass(currentBuffer);
        }

        public static void EndDraw()
        {
            Assert(_vk.EndCommandBuffer(currentBuffer));

            var imageAvailable = _semaphoreImageAvailable;
            var renderingDone = _semaphoreRenderingDone;

            PipelineStageFlags waitStage = PipelineStageFlags.PipelineStageColorAttachmentOutputBit;

            var buffer = currentBuffer;
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

            Assert(_vk.QueueSubmit(_graphicQueue, 1u, submitInfo, _renderFences[imageIndex]));

            var swapchain = _swapchain;
            var imageindex = imageIndex;
            PresentInfoKHR presentInfo = new()
            {
                SType = StructureType.PresentInfoKhr,
                WaitSemaphoreCount = 1,
                PWaitSemaphores = &renderingDone,
                PSwapchains = &swapchain,
                SwapchainCount = 1,
                PImageIndices = &imageindex
            };

            Assert(_vkSwapchain.QueuePresent(_presentQueue, presentInfo));
        }


        private static void CreateSyncStructures()
        {
            SemaphoreCreateInfo semaphoreCreateInfo = new()
            {
                SType = StructureType.SemaphoreCreateInfo
            };

            Assert(_vk.CreateSemaphore(_device, semaphoreCreateInfo, _allocationCallback,
                out _semaphoreImageAvailable));
            Assert(_vk.CreateSemaphore(_device, semaphoreCreateInfo, _allocationCallback,
                out _semaphoreRenderingDone));

            FenceCreateInfo fenceCreateInfo = new()
            {
                SType = StructureType.FenceCreateInfo,
                Flags = FenceCreateFlags.FenceCreateSignaledBit
            };

            _renderFences = new Fence[SwapchainImageCount];
            for (int i = 0; i < SwapchainImageCount; i++)
            {
                Assert(_vk.CreateFence(_device, fenceCreateInfo, _allocationCallback, out _renderFences[i]));
            }
        }


        private static void CreateSimplePipeline()
        {
            GraphicsPipelineBuilder builder = new();

            _triangleFragShader = Shader.CreateShader(File.ReadAllBytes("EngineResources/shaders/color_frag.spv"));
            _triangleVertShader = Shader.CreateShader(File.ReadAllBytes("EngineResources/shaders/triangle_vert.spv"));


            var fragShaderContainer =
                _triangleFragShader.GetShaderStageContainer(ShaderStageFlags.ShaderStageFragmentBit, "main");
            var vertShaderContainer =
                _triangleVertShader.GetShaderStageContainer(ShaderStageFlags.ShaderStageVertexBit, "main");
            var shader = stackalloc PipelineShaderStageCreateInfo[2];
            shader[0] = fragShaderContainer.ShaderStageCreateInfo;
            shader[1] = vertShaderContainer.ShaderStageCreateInfo;
            builder.SetShaderStages(shader, 2);

            var vertex = new DefaultVertex();
            var vertexInputAttributes = vertex.GetVertexAttributes();
            var vertexBindings = vertex.GetVertexBindings();

            var vertexAttributeHandle = GCHandle.Alloc(vertexInputAttributes, GCHandleType.Pinned);
            var vertexBindingHandle = GCHandle.Alloc(vertexBindings, GCHandleType.Pinned);

            PipelineVertexInputStateCreateInfo inputStateCreateInfo = new()
            {
                SType = StructureType.PipelineVertexInputStateCreateInfo,
                VertexAttributeDescriptionCount = (uint)vertexInputAttributes.Length,
                VertexBindingDescriptionCount = (uint)vertexBindings.Length,
                PVertexAttributeDescriptions =
                    (VertexInputAttributeDescription*)vertexAttributeHandle.AddrOfPinnedObject(),
                PVertexBindingDescriptions = (VertexInputBindingDescription*)vertexBindingHandle.AddrOfPinnedObject()
            };
            builder.SetVertexInputState(&inputStateCreateInfo);

            PipelineInputAssemblyStateCreateInfo assemblyStateCreateInfo = new()
            {
                SType = StructureType.PipelineInputAssemblyStateCreateInfo,
                PrimitiveRestartEnable = Vk.False,
                Topology = PrimitiveTopology.TriangleList
            };
            builder.SetInputAssemblyState(&assemblyStateCreateInfo);

            Viewport viewport = new()
            {
                MaxDepth = 1f,
                Width = _swapchainExtent.Width,
                Height = _swapchainExtent.Height
            };

            Rect2D scissor = new()
            {
                Extent = _swapchainExtent,
                Offset = new Offset2D(0, 0)
            };

            PipelineViewportStateCreateInfo viewportStateCreateInfo = new()
            {
                SType = StructureType.PipelineViewportStateCreateInfo,
                PScissors = &scissor,
                ScissorCount = 1,
                PViewports = &viewport,
                ViewportCount = 1
            };
            builder.SetViewportState(&viewportStateCreateInfo);

            PipelineRasterizationStateCreateInfo rasterizationStateCreateInfo = new()
            {
                SType = StructureType.PipelineRasterizationStateCreateInfo,
                CullMode = CullModeFlags.CullModeNone,
                FrontFace = FrontFace.Clockwise,
                RasterizerDiscardEnable = Vk.False,
                LineWidth = 1f,
                PolygonMode = PolygonMode.Fill,
                DepthBiasEnable = Vk.False,
                DepthClampEnable = Vk.False,
            };
            builder.SetRasterizationState(&rasterizationStateCreateInfo);

            PipelineMultisampleStateCreateInfo multisampleStateCreateInfo = new()
            {
                SType = StructureType.PipelineMultisampleStateCreateInfo,
                RasterizationSamples = SampleCountFlags.SampleCount1Bit,
                MinSampleShading = 1f,
                SampleShadingEnable = Vk.False,
                AlphaToCoverageEnable = Vk.False,
                AlphaToOneEnable = Vk.False
            };
            builder.SetMultiSampleState(&multisampleStateCreateInfo);

            PipelineColorBlendAttachmentState colorBlendAttachmentState = new()
            {
                BlendEnable = Vk.True,
                SrcColorBlendFactor = BlendFactor.SrcAlpha,
                DstColorBlendFactor = BlendFactor.OneMinusSrcAlpha,
                ColorBlendOp = BlendOp.Add,
                SrcAlphaBlendFactor = BlendFactor.One,
                DstAlphaBlendFactor = BlendFactor.Zero,
                AlphaBlendOp = BlendOp.Add,
                ColorWriteMask = ColorComponentFlags.ColorComponentRBit | ColorComponentFlags.ColorComponentGBit |
                                 ColorComponentFlags.ColorComponentBBit | ColorComponentFlags.ColorComponentABit
            };

            PipelineColorBlendStateCreateInfo colorBlendStateCreateInfo = new()
            {
                SType = StructureType.PipelineColorBlendStateCreateInfo,
                AttachmentCount = 1,
                PAttachments = &colorBlendAttachmentState,
                LogicOpEnable = Vk.False
            };
            colorBlendStateCreateInfo.BlendConstants[0] = 0f;
            colorBlendStateCreateInfo.BlendConstants[1] = 0f;
            colorBlendStateCreateInfo.BlendConstants[2] = 0f;
            colorBlendStateCreateInfo.BlendConstants[3] = 0f;
            builder.SetColorBlendState(&colorBlendStateCreateInfo);

            PipelineLayoutCreateInfo pipelineLayoutCreateInfo = new()
            {
                SType = StructureType.PipelineLayoutCreateInfo
            };
            Assert(_vk.CreatePipelineLayout(_device, pipelineLayoutCreateInfo, _allocationCallback,
                out _pipelineLayout));
            builder.SetLayout(_pipelineLayout);

            builder.SetRenderPass(MainRenderPass);

            _pipeline = builder.Build();

            fragShaderContainer.Dispose();
            vertShaderContainer.Dispose();

            vertexAttributeHandle.Free();
            vertexBindingHandle.Free();
        }

        private static void CreateCommandPool()
        {
            GraphicsCommandPool = new CommandPool[SwapchainImageCount];

            for (int i = 0; i < SwapchainImageCount; i++)
            {
                CommandPoolCreateInfo createInfo = new()
                {
                    SType = StructureType.CommandPoolCreateInfo,
                    QueueFamilyIndex = _queueFamilyIndexes.GraphicsFamily!.Value
                };
                _vk.CreateCommandPool(_device, createInfo, _allocationCallback, out GraphicsCommandPool[i]);
            }
        }

        private static void CreateMainRenderPass()
        {
            AttachmentDescription attachmentDescription = new()
            {
                Format = _swapchainImageFormat,
                Samples = SampleCountFlags.SampleCount1Bit,
                LoadOp = AttachmentLoadOp.Clear,
                StoreOp = AttachmentStoreOp.Store,
                StencilLoadOp = AttachmentLoadOp.DontCare,
                StencilStoreOp = AttachmentStoreOp.DontCare,
                InitialLayout = ImageLayout.Undefined,
                FinalLayout = ImageLayout.PresentSrcKhr
            };

            AttachmentReference attachmentReference = new()
            {
                Attachment = 0u,
                Layout = ImageLayout.ColorAttachmentOptimal
            };

            SubpassDescription subpassDescription = new()
            {
                PipelineBindPoint = PipelineBindPoint.Graphics,
                PInputAttachments = null,
                InputAttachmentCount = 0u,
                ColorAttachmentCount = 1,
                PColorAttachments = &attachmentReference
            };

            SubpassDependency subpassDependency = new()
            {
                DstSubpass = 0,
                SrcSubpass = Vk.SubpassExternal,
                SrcStageMask = PipelineStageFlags.PipelineStageColorAttachmentOutputBit,
                DstStageMask = PipelineStageFlags.PipelineStageColorAttachmentOutputBit,
                SrcAccessMask = AccessFlags.AccessNoneKhr,
                DstAccessMask = AccessFlags.AccessColorAttachmentWriteBit | AccessFlags.AccessColorAttachmentReadBit
            };

            RenderPassCreateInfo renderPassCreateInfo = new()
            {
                SType = StructureType.RenderPassCreateInfo,
                AttachmentCount = 1,
                PAttachments = &attachmentDescription,
                SubpassCount = 1,
                PSubpasses = &subpassDescription,
                DependencyCount = 1,
                PDependencies = &subpassDependency
            };

            _vk.CreateRenderPass(_device, renderPassCreateInfo, _allocationCallback, out MainRenderPass);
        }

        private static void CreateFramebuffer()
        {
            _swapchainFramebuffers = new Framebuffer[SwapchainImageCount];
            fixed (ImageView* imageView = &_swapchainImageViews[0])
            {
                for (int i = 0; i < SwapchainImageCount; i++)
                {
                    FramebufferCreateInfo createInfo = new()
                    {
                        SType = StructureType.FramebufferCreateInfo,
                        AttachmentCount = 1,
                        PAttachments = &imageView[i],
                        Height = _swapchainExtent.Height,
                        Width = _swapchainExtent.Width,
                        RenderPass = MainRenderPass,
                        Layers = 1
                    };

                    Assert(_vk.CreateFramebuffer(_device, createInfo, _allocationCallback,
                        out _swapchainFramebuffers[i]));
                }
            }
        }

        private static void CreateImageViews()
        {
            _swapchainImageViews = new ImageView[SwapchainImageCount];

            for (int i = 0; i < SwapchainImageCount; i++)
            {
                ImageViewCreateInfo createInfo = new()
                {
                    SType = StructureType.ImageViewCreateInfo,
                    Image = _swapchainImages[i],
                    ViewType = ImageViewType.ImageViewType2D,
                    Format = _swapchainImageFormat,
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

                Assert(_vk.CreateImageView(_device, createInfo, _allocationCallback, out _swapchainImageViews[i]));
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
                Surface = _surface,
                PreTransform = capabilities.CurrentTransform,
                CompositeAlpha = CompositeAlphaFlagsKHR.CompositeAlphaOpaqueBitKhr,
                Clipped = Vk.True,
                OldSwapchain = default,
                MinImageCount = imageCount
            };

            var indices = _queueFamilyIndexes;
            uint* queueFamilyIndices = stackalloc uint[2]
                { _queueFamilyIndexes.GraphicsFamily!.Value, _queueFamilyIndexes.PresentFamily!.Value };

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

            if (!_vk.TryGetDeviceExtension(_instance, _device, out _vkSwapchain))
            {
                Logger.WriteLog("KhrSwapchain extension not found", LogImportance.EXCEPTION, "Render");
            }

            Assert(_vkSwapchain.CreateSwapchain(_device, createInfo, _allocationCallback, out _swapchain));

            _vkSwapchain.GetSwapchainImages(_device, _swapchain, ref imageCount, null);
            _swapchainImages = new Image[imageCount];
            _vkSwapchain.GetSwapchainImages(_device, _swapchain, ref imageCount, out _swapchainImages[0]);

            _swapchainImageFormat = format.Format;
            _swapchainExtent = extent;
        }

        private static Extent2D GetSwapChainExtent(SurfaceCapabilitiesKHR swapchainSupportCapabilities)
        {
            if (swapchainSupportCapabilities.CurrentExtent.Width != uint.MaxValue)
            {
                return swapchainSupportCapabilities.CurrentExtent;
            }

            var actualExtent = new Extent2D
                { Height = (uint)_window.FramebufferSize.Y, Width = (uint)_window.FramebufferSize.X };
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
            _vkSurface.GetPhysicalDeviceSurfaceCapabilities(_physicalDevice, _surface, out var capabilities);

            uint surfaceFormatCount = 0;
            _vkSurface.GetPhysicalDeviceSurfaceFormats(_physicalDevice, _surface, ref surfaceFormatCount, null);
            SurfaceFormatKHR[] surfaceFormats = new SurfaceFormatKHR[surfaceFormatCount];
            _vkSurface.GetPhysicalDeviceSurfaceFormats(_physicalDevice, _surface, ref surfaceFormatCount,
                out surfaceFormats[0]);

            uint surfaceModeCount = 0;
            _vkSurface.GetPhysicalDeviceSurfacePresentModes(_physicalDevice, _surface, ref surfaceModeCount, null);
            PresentModeKHR[] presentModes = new PresentModeKHR[surfaceModeCount];
            _vkSurface.GetPhysicalDeviceSurfacePresentModes(_physicalDevice, _surface, ref surfaceModeCount,
                out presentModes[0]);

            return (capabilities, surfaceFormats, presentModes);
        }

        private static void CreateDevice()
        {
            Logger.WriteLog($"Creating device", LogImportance.DEBUG, "Render");
            _physicalDevice = ChoosePhysicalDevice(EnumerateDevices(_instance));
            _queueFamilyIndexes = GetQueueFamilyIndexes(_physicalDevice);

            uint queueCount = _queueFamilyIndexes.GraphicsFamily!.Value != _queueFamilyIndexes.ComputeFamily!.Value
                ? 2u
                : 1u;
            if (_queueFamilyIndexes.GraphicsFamily!.Value != _queueFamilyIndexes.PresentFamily!.Value) queueCount++;
            DeviceQueueCreateInfo* queueCreateInfo = stackalloc DeviceQueueCreateInfo[(int)queueCount];
            float priority = 1f;

            queueCreateInfo[0] = new DeviceQueueCreateInfo()
            {
                SType = StructureType.DeviceQueueCreateInfo,
                QueueCount = 1,
                QueueFamilyIndex = _queueFamilyIndexes.GraphicsFamily!.Value,
                PQueuePriorities = &priority
            };
            queueCreateInfo[_queueFamilyIndexes.GraphicsFamily == _queueFamilyIndexes.ComputeFamily ? 0 : 1] = new()
            {
                SType = StructureType.DeviceQueueCreateInfo,
                QueueCount = 1,
                QueueFamilyIndex = _queueFamilyIndexes.ComputeFamily!.Value,
                PQueuePriorities = &priority
            };
            queueCreateInfo
                [_queueFamilyIndexes.GraphicsFamily == _queueFamilyIndexes.PresentFamily ? 0 : queueCount - 1] = new()
            {
                SType = StructureType.DeviceQueueCreateInfo,
                QueueCount = 1,
                QueueFamilyIndex = _queueFamilyIndexes.PresentFamily!.Value,
                PQueuePriorities = &priority
            };

            DeviceCreateInfo deviceCreateInfo = new()
            {
                SType = StructureType.DeviceCreateInfo,
                PQueueCreateInfos = queueCreateInfo,
                QueueCreateInfoCount = queueCount
            };

            var extensions = new HashSet<string>(EnumerateDeviceExtensions(_physicalDevice));
            foreach (var extension in _deviceExtensions)
            {
                if (!extensions.Contains(extension))
                {
                    throw new MintyCoreException($"Missing device extension {extension}");
                }
            }

            deviceCreateInfo.EnabledExtensionCount = (uint)_deviceExtensions.Length;
            deviceCreateInfo.PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr(_deviceExtensions);

            Assert(_vk.CreateDevice(_physicalDevice, deviceCreateInfo, _allocationCallback, out _device));
            SilkMarshal.Free((nint)deviceCreateInfo.PpEnabledExtensionNames);

            _vk.GetDeviceQueue(_device, _queueFamilyIndexes.GraphicsFamily.Value, 0, out _graphicQueue);
            _vk.GetDeviceQueue(_device, _queueFamilyIndexes.ComputeFamily.Value, 0, out _computeQueue);
            _vk.GetDeviceQueue(_device, _queueFamilyIndexes.PresentFamily.Value, 0, out _presentQueue);
        }

        private static QueueFamilyIndexes GetQueueFamilyIndexes(PhysicalDevice device)
        {
            QueueFamilyIndexes indexes = default;

            uint queueFamilyCount = 0;
            _vk.GetPhysicalDeviceQueueFamilyProperties(device, &queueFamilyCount, null);
            QueueFamilyProperties[] queueFamilyProperties = new QueueFamilyProperties[queueFamilyCount];
            _vk.GetPhysicalDeviceQueueFamilyProperties(device, &queueFamilyCount, out queueFamilyProperties[0]);

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

                _vkSurface.GetPhysicalDeviceSurfaceSupport(device, i, _surface, out var presentSupport);

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
                _vk.GetPhysicalDeviceProperties(devices[i], out deviceProperties[i]);
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
            if (!_vk.TryGetInstanceExtension(_instance, out _vkSurface))
            {
                throw new MintyCoreException("KHR_surface extension not found.");
            }

            _surface = Engine.Window!.GetWindow().VkSurface!.Create(_instance.ToHandle(), _allocationCallback)
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
                _validationLayersActive = false;
            }

            if (_validationLayersActive)
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

            Assert(_vk.CreateInstance(createInfo, _allocationCallback, out _instance));
            _vk.CurrentInstance = _instance;

            SilkMarshal.Free((nint)createInfo.PpEnabledLayerNames);
        }

        private static void Resized(Vector2D<int> obj)
        {
            throw new NotImplementedException();
        }

        private static uint currentImageIndex;

        internal static void Shutdown()
        {
            Logger.WriteLog("Shutdown Vulkan", LogImportance.INFO, "Render");
            _vk.DeviceWaitIdle(_device);

            triangleMesh.Dispose();
            foreach (var fence in _renderFences)
            {
                _vk.DestroyFence(_device, fence, _allocationCallback);
            }

            _vk.DestroySemaphore(_device, _semaphoreImageAvailable, _allocationCallback);
            _vk.DestroySemaphore(_device, _semaphoreRenderingDone, _allocationCallback);

            _vk.DestroyPipeline(_device, _pipeline, _allocationCallback);
            _vk.DestroyPipelineLayout(_device, _pipelineLayout, _allocationCallback);
            _triangleFragShader.Dispose();
            _triangleVertShader.Dispose();

            foreach (var commandPool in GraphicsCommandPool)
            {
                _vk.DestroyCommandPool(_device, commandPool, _allocationCallback);
            }

            foreach (var framebuffer in _swapchainFramebuffers)
            {
                _vk.DestroyFramebuffer(_device, framebuffer, _allocationCallback);
            }

            _vk.DestroyRenderPass(_device, MainRenderPass, _allocationCallback);
            foreach (var imageView in _swapchainImageViews)
            {
                _vk.DestroyImageView(_device, imageView, _allocationCallback);
            }

            _vkSwapchain.DestroySwapchain(_device, _swapchain, _allocationCallback);
            _vk.DestroyDevice(_device, _allocationCallback);
            _vkSurface.DestroySurface(_instance, _surface, _allocationCallback);
            _vk.DestroyInstance(_instance, _allocationCallback);
        }
    }
}