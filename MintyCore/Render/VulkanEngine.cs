using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using JetBrains.Annotations;
using MintyCore.Identifications;
using MintyCore.Utils;
using Silk.NET.Core.Native;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using VkSemaphore = Silk.NET.Vulkan.Semaphore;
using static MintyCore.Render.VulkanUtils;

namespace MintyCore.Render;

/// <summary>
///     Base class to interact with the VulkanAPI through the Silk.Net Library
///     <remarks>You wont find a documentation how to use vulkan here</remarks>
/// </summary>
[PublicAPI]
public static unsafe class VulkanEngine
{
    private static bool _validationLayerOverride = true;
    private static bool ValidationLayersActive => Engine.TestingModeActive && _validationLayerOverride;


    /// <summary>
    ///     Access point to the vulkan api
    /// </summary>
    public static readonly Vk Vk = Vk.GetApi();

    /// <summary>
    ///     Allocation callbacks for vulkan (currently null, used for easy implementation of custom allocators in the future)
    /// </summary>
    public static readonly AllocationCallbacks* AllocationCallback = null;

    /// <summary>
    ///     The current vulkan instance
    /// </summary>
    public static Instance Instance { get; private set; }

    /// <summary>
    ///     The current vulkan surface
    /// </summary>
    public static SurfaceKHR Surface { get; private set; }

    /// <summary>
    ///     The current vulkan physical device
    /// </summary>
    public static PhysicalDevice PhysicalDevice { get; private set; }

    /// <summary>
    ///     The current vulkan physical device memory properties
    /// </summary>
    public static PhysicalDeviceMemoryProperties PhysicalDeviceMemoryProperties { get; private set; }

    /// <summary>
    ///     The vulkan logical device
    /// </summary>
    public static Device Device { get; private set; }

    /// <summary>
    ///     The information about queue family indices
    /// </summary>
    public static QueueFamilyIndexes QueueFamilyIndexes { get; private set; }

    /// <summary>
    ///     The vulkan graphics queue
    /// </summary>
    public static Queue GraphicQueue { get; private set; }

    /// <summary>
    ///     The vulkan present queue (possible the same as <see cref="GraphicQueue" />)
    /// </summary>
    public static Queue PresentQueue { get; private set; }

    /// <summary>
    ///     The vulkan compute queue
    /// </summary>
    public static Queue ComputeQueue { get; private set; }

    /// <summary>
    ///     The vulkan surface api access
    /// </summary>
    // ReSharper disable once NotNullMemberIsNotInitialized
    public static KhrSurface? VkSurface { get; private set; }

    /// <summary>
    ///     The vulkan swapchain api access
    /// </summary>
    // ReSharper disable once NotNullMemberIsNotInitialized
    public static KhrSwapchain? VkSwapchain { get; private set; }

    /// <summary>
    ///     The vulkan swapchain
    /// </summary>
    public static SwapchainKHR Swapchain { get; private set; }

    /// <summary>
    ///     The vulkan swapchain images
    /// </summary>
    public static Image[] SwapchainImages { get; private set; } = Array.Empty<Image>();

    /// <summary>
    ///     The swapchain image format
    /// </summary>
    public static Format SwapchainImageFormat { get; private set; }

    /// <summary>
    ///     The swapchain extent (size)
    /// </summary>
    public static Extent2D SwapchainExtent { get; private set; }

    /// <summary>
    ///     The swapchain image views
    /// </summary>
    public static ImageView[] SwapchainImageViews { get; private set; } = Array.Empty<ImageView>();

    /// <summary>
    ///     The swapchain image count.
    ///     Useful if you want to have per frame data on the gpu (like dynamic data)
    /// </summary>
    public static int SwapchainImageCount => SwapchainImages.Length;

    /// <summary>
    ///     The depth texture
    /// </summary>
    public static Texture DepthTexture;

    /// <summary>
    ///     The depth image view
    /// </summary>
    public static ImageView DepthImageView;

    /// <summary>
    ///     the framebuffers of the swap chains
    /// </summary>
    public static Framebuffer[] SwapchainFramebuffers = Array.Empty<Framebuffer>();

    /// <summary>
    ///     Command pools  for graphic commands
    /// </summary>
    public static CommandPool[] GraphicsCommandPool = Array.Empty<CommandPool>();

    /// <summary>
    ///     Command pool for single time command buffers
    /// </summary>
    private static CommandPool _singleTimeCommandPool;

    /// <summary>
    ///     A queue of allocated single time command buffers
    /// </summary>
    private static readonly Queue<CommandBuffer> _singleTimeCommandBuffers = new();

    private static VkSemaphore _semaphoreImageAvailable;
    private static VkSemaphore _semaphoreRenderingDone;
    private static Fence[] _renderFences = Array.Empty<Fence>();

    /// <summary>
    ///     Whether or not drawing is enabled
    /// </summary>
    public static bool DrawEnable { get; private set; }

    private static readonly Thread _mainThread = Thread.CurrentThread;

    internal static void Setup()
    {
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

        Engine.Window!.WindowInstance.FramebufferResize += Resized;
    }


    private static CommandBuffer[] _graphicsMainCommandBuffer = Array.Empty<CommandBuffer>();
    private static RenderPass? _activeRenderPass;

    private static Queue<CommandBuffer>[] _availableGraphicsSecondaryCommandBufferPool =
        Array.Empty<Queue<CommandBuffer>>();

    private static Queue<CommandBuffer>[] _usedGraphicsSecondaryCommandBufferPool = Array.Empty<Queue<CommandBuffer>>();

    /// <summary>
    ///     The current Image index
    /// </summary>
    public static uint ImageIndex { get; private set; }

    /// <summary>
    ///     Prepare the current frame for drawing
    /// </summary>
    /// <returns>True if the next image could be acquired. If false do no rendering</returns>
    public static bool PrepareDraw()
    {
        AssertVulkanInstance();
        Logger.AssertAndThrow(VkSwapchain is not null, "KhrSwapchain extension is null", "Renderer");


        var frameBufferSize = Engine.Window!.WindowInstance.FramebufferSize;
        if (frameBufferSize.X == 0 || frameBufferSize.Y == 0)
            return false;

        Result acquireResult;

        do
        {
            uint imageIndex = 0;
            acquireResult = VkSwapchain.AcquireNextImage(Device, Swapchain, ulong.MaxValue, _semaphoreImageAvailable,
                default,
                ref imageIndex);
            ImageIndex = imageIndex;

            if (acquireResult != Result.Success) RecreateSwapchain();
        } while (acquireResult != Result.Success);

        Assert(Vk.WaitForFences(Device, _renderFences.AsSpan((int) ImageIndex, 1), Vk.True, ulong.MaxValue));
        Assert(Vk.ResetFences(Device, _renderFences.AsSpan((int) ImageIndex, 1)));
        Assert(Vk.ResetCommandPool(Device, GraphicsCommandPool[ImageIndex],
            CommandPoolResetFlags.CommandPoolResetReleaseResourcesBit));

        while (_usedGraphicsSecondaryCommandBufferPool[ImageIndex].TryDequeue(out var buffer))
            _availableGraphicsSecondaryCommandBufferPool[ImageIndex].Enqueue(buffer);

        CommandBufferBeginInfo beginInfo = new()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.CommandBufferUsageOneTimeSubmitBit
        };
        Assert(Vk.BeginCommandBuffer(_graphicsMainCommandBuffer[ImageIndex], beginInfo));

        var clearValues = stackalloc ClearValue[]
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
            Framebuffer = SwapchainFramebuffers[ImageIndex],
            RenderPass = RenderPassHandler.GetRenderPass(RenderPassIDs.Initial),
            RenderArea = new Rect2D
            {
                Extent = SwapchainExtent,
                Offset = new Offset2D(0, 0)
            },
            ClearValueCount = 2,
            PClearValues = clearValues
        };
        Vk.CmdBeginRenderPass(_graphicsMainCommandBuffer[ImageIndex], renderPassBeginInfo,
            SubpassContents.SecondaryCommandBuffers);

        _activeRenderPass = RenderPassHandler.GetRenderPass(RenderPassIDs.Initial);

        DrawEnable = true;
        return true;
    }

    /// <summary>
    ///     Get secondary command buffer for rendering
    ///     CommandBuffers acquired with this method are only valid for the current frame and be returned to the internal pool
    /// </summary>
    /// <param name="beginBuffer">Whether or not the buffer should be started</param>
    /// <param name="inheritRenderPass">Whether or not the render pass should be inherited</param>
    /// <returns>Secondary command buffer</returns>
    public static CommandBuffer GetSecondaryCommandBuffer( bool beginBuffer = true, bool inheritRenderPass = true, RenderPass renderPass = default, uint subpass = 0)
    {
        AssertVulkanInstance();

        Logger.AssertAndThrow(Thread.CurrentThread == _mainThread,
            "Tried to get secondary command buffer from a multi threaded context", "Render");

        Logger.AssertAndThrow(DrawEnable, "Tried to create secondary command buffer, while drawing is disabled",
            "Render");


        if (!_availableGraphicsSecondaryCommandBufferPool[ImageIndex].TryDequeue(out var buffer))
        {
            CommandBufferAllocateInfo allocateInfo = new()
            {
                SType = StructureType.CommandBufferAllocateInfo,
                Level = CommandBufferLevel.Secondary,
                CommandPool = GraphicsCommandPool[ImageIndex],
                CommandBufferCount = 1
            };

            Assert(Vk.AllocateCommandBuffers(Device, allocateInfo, out buffer));
        }

        _usedGraphicsSecondaryCommandBufferPool[ImageIndex].Enqueue(buffer);

        if (!beginBuffer) return buffer;

        CommandBufferInheritanceInfo inheritanceInfo = new()
        {
            SType = StructureType.CommandBufferInheritanceInfo,
            RenderPass = renderPass.Handle == default ? RenderPassHandler.MainRenderPass : renderPass,
            Subpass = subpass
        };
        CommandBufferBeginInfo beginInfo = new()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = inheritRenderPass
                ? CommandBufferUsageFlags.CommandBufferUsageOneTimeSubmitBit |
                  CommandBufferUsageFlags.CommandBufferUsageRenderPassContinueBit
                : CommandBufferUsageFlags.CommandBufferUsageOneTimeSubmitBit,
            PInheritanceInfo = inheritRenderPass ? &inheritanceInfo : null
        };
        Assert(Vk.BeginCommandBuffer(buffer, beginInfo));
        return buffer;
    }

    /// <summary>
    ///     Execute a secondary command buffer on the graphics command buffer
    /// </summary>
    /// <param name="buffer">Command buffer to execute</param>
    /// <param name="endBuffer">Whether or not the command buffer need to be ended</param>
    public static void ExecuteSecondary(CommandBuffer buffer, bool endBuffer = true)
    {
        AssertVulkanInstance();
        Logger.AssertAndThrow(Thread.CurrentThread == _mainThread,
            "Secondary command buffers can only be executed in the main command buffer from the main thread, to ensure proper synchronization",
            "Render");

        if (endBuffer) Vk.EndCommandBuffer(buffer);
        Vk.CmdExecuteCommands(_graphicsMainCommandBuffer[ImageIndex], 1, buffer);
    }

    /// <summary>
    /// Set the currently active render pass for the main command buffer
    /// </summary>
    /// <param name="renderPass"><see cref="RenderPassBeginInfo.RenderPass"/></param>
    /// <param name="subpassContents"></param>
    /// <param name="clearValues"><see cref="RenderPassBeginInfo.PClearValues"/></param>
    /// <param name="renderArea"><see cref="RenderPassBeginInfo.RenderArea"/></param>
    /// <param name="framebuffer"><see cref="RenderPassBeginInfo.Framebuffer"/></param>
    public static void SetActiveRenderPass(RenderPass renderPass, SubpassContents subpassContents,
        Span<ClearValue> clearValues = default,
        Rect2D? renderArea = null, Framebuffer? framebuffer = null)
    {
        if (_activeRenderPass is not null)
        {
            Vk.CmdEndRenderPass(_graphicsMainCommandBuffer[ImageIndex]);
        }

        RenderPassBeginInfo beginInfo = new()
        {
            SType = StructureType.RenderPassBeginInfo,
            RenderPass = renderPass,
            ClearValueCount = (uint) clearValues.Length,
            PClearValues = clearValues.Length != 0
                ? (ClearValue*) Unsafe.AsPointer(ref clearValues.GetPinnableReference())
                : null,
            RenderArea = renderArea ?? new Rect2D
            {
                Extent = SwapchainExtent,
                Offset = new Offset2D(0, 0)
            },
            Framebuffer = framebuffer ?? SwapchainFramebuffers[ImageIndex]
        };
        Vk.CmdBeginRenderPass(_graphicsMainCommandBuffer[ImageIndex], beginInfo, subpassContents);

        _activeRenderPass = renderPass;
    }

    /// <summary>
    /// Increase to the next subpass of the currently active render pass
    /// </summary>
    /// <param name="subPassContents"></param>
    public static void NextSubPass(SubpassContents subPassContents)
    {
        Logger.AssertAndThrow(_activeRenderPass is not null, "Tried to call NextSubPass without an active render pass",
            "Renderer");

        Vk.CmdNextSubpass(_graphicsMainCommandBuffer[ImageIndex], subPassContents);
    }

    /// <summary>
    ///     End the draw of the current frame
    /// </summary>
    public static void EndDraw()
    {
        AssertVulkanInstance();
        Logger.AssertAndThrow(VkSwapchain is not null, "KhrSwapchain extension is null", "Renderer");

        DrawEnable = false;
        if (_activeRenderPass is not null)
        {
            Vk.CmdEndRenderPass(_graphicsMainCommandBuffer[ImageIndex]);
            _activeRenderPass = null;
        }

        Assert(Vk.EndCommandBuffer(_graphicsMainCommandBuffer[ImageIndex]));

        var imageAvailable = _semaphoreImageAvailable;
        var renderingDone = _semaphoreRenderingDone;

        var waitStage = PipelineStageFlags.PipelineStageColorAttachmentOutputBit;

        var buffer = _graphicsMainCommandBuffer[ImageIndex];
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

        Assert(Vk.QueueSubmit(GraphicQueue, 1u, submitInfo, _renderFences[ImageIndex]));

        var swapchain = Swapchain;
        var imageIndex = ImageIndex;
        PresentInfoKHR presentInfo = new()
        {
            SType = StructureType.PresentInfoKhr,
            WaitSemaphoreCount = 1,
            PWaitSemaphores = &renderingDone,
            PSwapchains = &swapchain,
            SwapchainCount = 1,
            PImageIndices = &imageIndex
        };

        VkSwapchain.QueuePresent(PresentQueue, presentInfo);
    }

    private static void CreateRenderFence()
    {
        AssertVulkanInstance();

        FenceCreateInfo fenceCreateInfo = new()
        {
            SType = StructureType.FenceCreateInfo,
            Flags = FenceCreateFlags.FenceCreateSignaledBit
        };

        _renderFences = new Fence[SwapchainImageCount];
        for (var i = 0; i < SwapchainImageCount; i++)
            Assert(Vk.CreateFence(Device, fenceCreateInfo, AllocationCallback, out _renderFences[i]));
    }

    private static void CreateRenderSemaphore()
    {
        AssertVulkanInstance();

        SemaphoreCreateInfo semaphoreCreateInfo = new()
        {
            SType = StructureType.SemaphoreCreateInfo
        };

        Assert(Vk.CreateSemaphore(Device, semaphoreCreateInfo, AllocationCallback,
            out _semaphoreImageAvailable));
        Assert(Vk.CreateSemaphore(Device, semaphoreCreateInfo, AllocationCallback,
            out _semaphoreRenderingDone));
    }

    private static void CreateCommandPool()
    {
        AssertVulkanInstance();

        GraphicsCommandPool = new CommandPool[SwapchainImageCount];
        CommandPoolCreateInfo createInfo = new()
        {
            SType = StructureType.CommandPoolCreateInfo,
            QueueFamilyIndex = QueueFamilyIndexes.GraphicsFamily!.Value
        };

        for (var i = 0; i < SwapchainImageCount; i++)
            Assert(Vk.CreateCommandPool(Device, createInfo, AllocationCallback, out GraphicsCommandPool[i]));

        createInfo.Flags = CommandPoolCreateFlags.CommandPoolCreateResetCommandBufferBit;
        Vk.CreateCommandPool(Device, createInfo, AllocationCallback, out _singleTimeCommandPool);

        _availableGraphicsSecondaryCommandBufferPool = new Queue<CommandBuffer>[SwapchainImageCount];
        _usedGraphicsSecondaryCommandBufferPool = new Queue<CommandBuffer>[SwapchainImageCount];

        _graphicsMainCommandBuffer = new CommandBuffer[SwapchainImageCount];

        for (var i = 0; i < SwapchainImageCount; i++)
        {
            CommandBufferAllocateInfo allocateInfo = new()
            {
                SType = StructureType.CommandBufferAllocateInfo,
                CommandBufferCount = 1,
                CommandPool = GraphicsCommandPool[i],
                Level = CommandBufferLevel.Primary
            };
            Assert(Vk.AllocateCommandBuffers(Device, allocateInfo, out _graphicsMainCommandBuffer[i]));

            _availableGraphicsSecondaryCommandBufferPool[i] = new Queue<CommandBuffer>();
            _usedGraphicsSecondaryCommandBufferPool[i] = new Queue<CommandBuffer>();
        }
    }

    private static void CreateFramebuffer()
    {
        AssertVulkanInstance();

        SwapchainFramebuffers = new Framebuffer[SwapchainImageCount];
        var imageViews = stackalloc ImageView[2];

        imageViews[1] = DepthImageView;

        for (var i = 0; i < SwapchainImageCount; i++)
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
        AssertVulkanInstance();

        SwapchainImageViews = new ImageView[SwapchainImageCount];

        for (var i = 0; i < SwapchainImageCount; i++)
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
        AssertVulkanInstance();

        Logger.WriteLog("Creating swapchain", LogImportance.Debug, "Render");

        var result = TryGetSwapChainSupport(out var support);
        Logger.AssertAndThrow(result, "Failed to get swapchain support information's", "Render");

        //Deconstruct the tuple into the single values
        var (capabilities, formats, presentModes) = support;

        var format = formats.FirstOrDefault(x => x.Format == Format.B8G8R8A8Unorm, formats[0]);

        var presentMode = presentModes.Contains(PresentModeKHR.PresentModeMailboxKhr)
            ? PresentModeKHR.PresentModeMailboxKhr
            : presentModes[0];

        var extent = GetSwapChainExtent(capabilities);

        var imageCount = capabilities.MinImageCount + 1;
        if (capabilities.MaxImageCount > 0 &&
            imageCount > capabilities.MaxImageCount)
            imageCount = capabilities.MaxImageCount;

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
        var queueFamilyIndices = stackalloc uint[2]
            {QueueFamilyIndexes.GraphicsFamily!.Value, QueueFamilyIndexes.PresentFamily!.Value};

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

        Logger.AssertAndThrow(Vk.TryGetDeviceExtension(Instance, Device, out KhrSwapchain khrSwapchain),
            "KhrSwapchain extension not found", "Render");

        VkSwapchain = khrSwapchain;

        Assert(VkSwapchain.CreateSwapchain(Device, createInfo, AllocationCallback, out var swapchain));
        Swapchain = swapchain;

        VkSwapchain.GetSwapchainImages(Device, Swapchain, ref imageCount, null);
        SwapchainImages = new Image[imageCount];
        VkSwapchain.GetSwapchainImages(Device, Swapchain, ref imageCount, out SwapchainImages[0]);

        SwapchainImageFormat = format.Format;
        SwapchainExtent = extent;
    }

    private static void RecreateSwapchain()
    {
        AssertVulkanInstance();

        Vk.DeviceWaitIdle(Device);
        CleanupSwapchain();

        CreateSwapchain();
        CreateSwapchainImageViews();
        CreateDepthBuffer();
        RenderPassHandler.CreateMainRenderPass(SwapchainImageFormat);
        CreateFramebuffer();

        Vk.DestroySemaphore(Device, _semaphoreImageAvailable, AllocationCallback);
        SemaphoreCreateInfo createInfo = new()
        {
            SType = StructureType.SemaphoreCreateInfo
        };
        Assert(Vk.CreateSemaphore(Device, createInfo, AllocationCallback, out _semaphoreImageAvailable));
    }

    private static void CreateDepthBuffer()
    {
        AssertVulkanInstance();
        var description = TextureDescription.Texture2D(SwapchainExtent.Width, SwapchainExtent.Height,
            1, 1, Format.D32Sfloat, TextureUsage.DepthStencil);
        description.AdditionalUsageFlags = ImageUsageFlags.ImageUsageInputAttachmentBit;
        DepthTexture = Texture.Create(ref description);

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
            }
        };

        Assert(Vk.CreateImageView(Device, createInfo, AllocationCallback, out DepthImageView));
    }

    private static Extent2D GetSwapChainExtent(SurfaceCapabilitiesKHR swapchainSupportCapabilities)
    {
        AssertVulkanInstance();
        if (swapchainSupportCapabilities.CurrentExtent.Width != uint.MaxValue)
            return swapchainSupportCapabilities.CurrentExtent;

        if (Engine.Window is null)
            return default;

        var actualExtent = new Extent2D
        {
            Height = (uint) Engine.Window.WindowInstance.FramebufferSize.Y,
            Width = (uint) Engine.Window.WindowInstance.FramebufferSize.X
        };
        actualExtent.Width = new[]
        {
            swapchainSupportCapabilities.MinImageExtent.Width,
            new[] {swapchainSupportCapabilities.MaxImageExtent.Width, actualExtent.Width}.Min()
        }.Max();
        actualExtent.Height = new[]
        {
            swapchainSupportCapabilities.MinImageExtent.Height,
            new[] {swapchainSupportCapabilities.MaxImageExtent.Height, actualExtent.Height}.Min()
        }.Max();

        return actualExtent;
    }

    private static bool TryGetSwapChainSupport(
        out (SurfaceCapabilitiesKHR, SurfaceFormatKHR[], PresentModeKHR[]) support)
    {
        support = (default, Array.Empty<SurfaceFormatKHR>(), Array.Empty<PresentModeKHR>());
        if (VkSurface is null) return false;

        VkSurface.GetPhysicalDeviceSurfaceCapabilities(PhysicalDevice, Surface, out var capabilities);

        uint surfaceFormatCount = 0;
        VkSurface.GetPhysicalDeviceSurfaceFormats(PhysicalDevice, Surface, ref surfaceFormatCount, null);
        var surfaceFormats = new SurfaceFormatKHR[surfaceFormatCount];
        VkSurface.GetPhysicalDeviceSurfaceFormats(PhysicalDevice, Surface, ref surfaceFormatCount,
            out surfaceFormats[0]);

        uint surfaceModeCount = 0;
        VkSurface.GetPhysicalDeviceSurfacePresentModes(PhysicalDevice, Surface, ref surfaceModeCount, null);
        var presentModes = new PresentModeKHR[surfaceModeCount];
        VkSurface.GetPhysicalDeviceSurfacePresentModes(PhysicalDevice, Surface, ref surfaceModeCount,
            out presentModes[0]);

        support = (capabilities, surfaceFormats, presentModes);
        return true;
    }

    private static readonly List<(string modName, string extensionName, bool hardRequirement)> _deviceExtensions = new()
    {
        ("Engine", KhrSwapchain.ExtensionName, true),
        ("Engine", KhrGetMemoryRequirements2.ExtensionName, true),
        ("Engine", "VK_KHR_dedicated_allocation", true)
    };

    /// <summary>
    /// Add a device extension
    /// </summary>
    /// <param name="modName">The mod adding the extension</param>
    /// <param name="extensionName">The name of the extension</param>
    /// <param name="hardRequirement">Whether the extension is a hard requirement. A exception will be thrown if the extension is not found</param>
    public static void AddDeviceExtension(string modName, string extensionName, bool hardRequirement)
    {
        _deviceExtensions.Add((modName, extensionName, hardRequirement));
    }

    /// <summary>
    /// List of loaded device extensions
    /// </summary>
    public static IReadOnlySet<string> LoadedDeviceExtensions { get; private set; } = new HashSet<string>();

    private static readonly List<IntPtr> _deviceFeatureExtensions = new();

    /// <summary>
    ///  Add a device feature extension
    /// </summary>
    /// <param name="extension"> The extension to add</param>
    /// <typeparam name="TExtension"> The type of the extension</typeparam>
    public static void AddDeviceFeatureExension<TExtension>(TExtension extension)
        where TExtension : unmanaged, IChainable
    {
        var copiedExtension = (TExtension*) AllocationHandler.Malloc<TExtension>();
        *copiedExtension = extension;
        _deviceFeatureExtensions.Add((IntPtr) copiedExtension);
    }

    /// <summary>
    /// Event called right before the device is created
    /// Remember to unsubscribe to don't break mod unloading
    /// </summary>
    public static event Action OnDeviceCreation = delegate { };

    private static void CreateDevice()
    {
        OnDeviceCreation();
        Logger.WriteLog("Creating device", LogImportance.Debug, "Render");
        PhysicalDevice = ChoosePhysicalDevice(EnumerateDevices(Instance));


        Vk.GetPhysicalDeviceMemoryProperties(PhysicalDevice, out var memoryProperties);
        PhysicalDeviceMemoryProperties = memoryProperties;

        QueueFamilyIndexes = GetQueueFamilyIndexes(PhysicalDevice);

        var queueCount = QueueFamilyIndexes.GraphicsFamily!.Value != QueueFamilyIndexes.ComputeFamily!.Value
            ? 2u
            : 1u;
        if (QueueFamilyIndexes.GraphicsFamily!.Value != QueueFamilyIndexes.PresentFamily!.Value) queueCount++;
        var queueCreateInfo = stackalloc DeviceQueueCreateInfo[(int) queueCount];
        var priority = 1f;

        queueCreateInfo[0] = new DeviceQueueCreateInfo
        {
            SType = StructureType.DeviceQueueCreateInfo,
            QueueCount = 1,
            QueueFamilyIndex = QueueFamilyIndexes.GraphicsFamily!.Value,
            PQueuePriorities = &priority
        };
        queueCreateInfo[QueueFamilyIndexes.GraphicsFamily == QueueFamilyIndexes.ComputeFamily ? 0 : 1] =
            new DeviceQueueCreateInfo
            {
                SType = StructureType.DeviceQueueCreateInfo,
                QueueCount = 1,
                QueueFamilyIndex = QueueFamilyIndexes.ComputeFamily!.Value,
                PQueuePriorities = &priority
            };
        queueCreateInfo
                [QueueFamilyIndexes.GraphicsFamily == QueueFamilyIndexes.PresentFamily ? 0 : queueCount - 1] =
            new DeviceQueueCreateInfo
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

        var lastExtension = (MinimalExtension*) &deviceCreateInfo;
        foreach (var extension in _deviceFeatureExtensions)
        {
            var extensionPtr = (MinimalExtension*) extension;
            extensionPtr->PNext = null;
            lastExtension->PNext = extensionPtr;
            lastExtension = extensionPtr;
        }

        var availableExtensions = new HashSet<string>(EnumerateDeviceExtensions(PhysicalDevice));
        var extensions = new List<string>();

        foreach (var (modName, extension, hardRequirement) in _deviceExtensions)
        {
            if (!availableExtensions.Contains(extension))
            {
                if (hardRequirement)
                {
                    Logger.WriteLog(
                        $"Device extension {extension} is not available. Requested by mod {modName}",
                        LogImportance.Exception, "Render");
                }
                else
                {
                    Logger.WriteLog(
                        $"Optional device extension {extension} is not available. Requested by mod {modName}",
                        LogImportance.Warning, "Render");
                }

                continue;
            }

            extensions.Add(extension);
        }

        deviceCreateInfo.EnabledExtensionCount = (uint) extensions.Count;
        deviceCreateInfo.PpEnabledExtensionNames = (byte**) SilkMarshal.StringArrayToPtr(extensions);

        Assert(Vk.CreateDevice(PhysicalDevice, deviceCreateInfo, AllocationCallback, out var device));
        Device = device;
        SilkMarshal.Free((nint) deviceCreateInfo.PpEnabledExtensionNames);

        Vk.GetDeviceQueue(Device, QueueFamilyIndexes.GraphicsFamily.Value, 0, out var graphicQueue);
        Vk.GetDeviceQueue(Device, QueueFamilyIndexes.ComputeFamily.Value, 0, out var computeQueue);
        Vk.GetDeviceQueue(Device, QueueFamilyIndexes.PresentFamily.Value, 0, out var presentQueue);

        GraphicQueue = graphicQueue;
        ComputeQueue = computeQueue;
        PresentQueue = presentQueue;

        LoadedDeviceExtensions = new HashSet<string>(extensions);

        foreach (var intPtr in _deviceFeatureExtensions)
        {
            AllocationHandler.Free(intPtr);
        }
    }

    private static QueueFamilyIndexes GetQueueFamilyIndexes(PhysicalDevice device)
    {
        Logger.AssertAndThrow(VkSurface is not null, "KhrSurface extension is null", "Renderer");

        QueueFamilyIndexes indexes = default;

        uint queueFamilyCount = 0;
        Vk.GetPhysicalDeviceQueueFamilyProperties(device, &queueFamilyCount, null);
        var queueFamilyProperties = new QueueFamilyProperties[queueFamilyCount];
        Vk.GetPhysicalDeviceQueueFamilyProperties(device, &queueFamilyCount, out queueFamilyProperties[0]);

        for (var i = 0u;
             i < queueFamilyCount && !indexes.GraphicsFamily.HasValue && !indexes.ComputeFamily.HasValue &&
             !indexes.PresentFamily.HasValue;
             i++)
        {
            var queueFamily = queueFamilyProperties[i];

            if (queueFamily.QueueFlags.HasFlag(QueueFlags.QueueGraphicsBit))
                indexes.GraphicsFamily = i;

            if (queueFamily.QueueFlags.HasFlag(QueueFlags.QueueComputeBit))
                indexes.ComputeFamily = i;

            VkSurface.GetPhysicalDeviceSurfaceSupport(device, i, Surface, out var presentSupport);

            if (presentSupport == Vk.True) indexes.PresentFamily = i;
        }


        return indexes;
    }

    private static PhysicalDevice ChoosePhysicalDevice(IReadOnlyList<PhysicalDevice> devices)
    {
        Logger.AssertAndThrow(devices.Count != 0, "No graphic device found", "Render");

        var deviceProperties = new PhysicalDeviceProperties[devices.Count];

        for (var i = 0; i < devices.Count; i++)
            Vk.GetPhysicalDeviceProperties(devices[i], out deviceProperties[i]);

        for (var i = 0; i < devices.Count; i++)
            if (deviceProperties[i].DeviceType == PhysicalDeviceType.DiscreteGpu)
                return devices[i];

        for (var i = 0; i < devices.Count; i++)
            if (deviceProperties[i].DeviceType == PhysicalDeviceType.IntegratedGpu)
                return devices[i];

        for (var i = 0; i < devices.Count; i++)
            if (deviceProperties[i].DeviceType == PhysicalDeviceType.VirtualGpu)
                return devices[i];


        return devices[0];
    }

    private static void CreateSurface()
    {
        Logger.WriteLog("Creating surface", LogImportance.Debug, "Render");
        Logger.AssertAndThrow(Vk.TryGetInstanceExtension(Instance, out KhrSurface vkSurface),
            "KHR_surface extension not found.", "Render");
        VkSurface = vkSurface;

        Surface = Engine.Window!.WindowInstance.VkSurface!.Create(Instance.ToHandle(), AllocationCallback)
            .ToSurface();
    }

    private static string[]? GetValidationLayers()
    {
        string[][] validationLayerNamesPriorityList =
        {
            new[] {"VK_LAYER_KHRONOS_validation"},
            new[] {"VK_LAYER_LUNARG_standard_validation"},
            new[]
            {
                "VK_LAYER_GOOGLE_threading",
                "VK_LAYER_LUNARG_parameter_validation",
                "VK_LAYER_LUNARG_object_tracker",
                "VK_LAYER_LUNARG_core_validation",
                "VK_LAYER_GOOGLE_unique_objects"
            }
        };

        var availableLayersName = EnumerateInstanceLayers();

        return validationLayerNamesPriorityList.FirstOrDefault(validationSet =>
            validationSet.All(validationName => availableLayersName.Contains(validationName)));
    }

    private static readonly List<(string requestingMod, string layer, bool hardRequirement)>
        _additionalInstanceLayers =
            new();

    private static readonly List<(string requestingMod, string extensions, bool hardRequirement)>
        _additionalInstanceExtensions = new();

    /// <summary>
    /// Add a layer to be used by the instance.
    /// </summary>
    /// <param name="modName"> The name of the mod requesting the layer.</param>
    /// <param name="layers"> The name of the layer.</param>
    /// <param name="hardRequirement"> Whether the layer is a hard requirement. If yes a exception will be thrown if the layer is not available.</param>
    public static void AddInstanceLayer(string modName, string layers, bool hardRequirement = true)
    {
        _additionalInstanceLayers.Add((modName, layers, hardRequirement));
    }

    /// <summary>
    /// Add an extension to be used by the instance.
    /// </summary>
    /// <param name="modName"> The name of the mod requesting the extension.</param>
    /// <param name="extensions"> The name of the extension.</param>
    /// <param name="hardRequirement"> Whether the extension is a hard requirement. If yes a exception will be thrown if the extension is not available.</param>
    public static void AddInstanceExtension(string modName, string extensions, bool hardRequirement = true)
    {
        _additionalInstanceExtensions.Add((modName, extensions, hardRequirement));
    }

    /// <summary>
    /// List of all loaded instance layers.
    /// </summary>
    public static IReadOnlySet<string> LoadedInstanceLayers { get; private set; } = new HashSet<string>();

    /// <summary>
    /// List of all loaded instance extensions.
    /// </summary>
    public static IReadOnlySet<string> LoadedInstanceExtensions { get; private set; } = new HashSet<string>();

    private struct MinimalExtension
    {
        [UsedImplicitly] public StructureType SType;
        [UsedImplicitly] public void* PNext;
    }

    private static readonly List<IntPtr> _instanceFeatureExtensions = new();

    /// <summary>
    /// Add a instance feature extension
    /// </summary>
    /// <param name="extension">The extension to use</param>
    /// <typeparam name="TExtension">The type of the extension</typeparam>
    public static void AddInstanceFeatureExtension<TExtension>(TExtension extension)
        where TExtension : unmanaged, IChainable
    {
        var copiedExtension = (TExtension*) AllocationHandler.Malloc<TExtension>();
        *copiedExtension = extension;
        _instanceFeatureExtensions.Add((IntPtr) copiedExtension);
    }

    private static void CreateInstance()
    {
        Logger.WriteLog("Creating instance", LogImportance.Debug, "Render");
        ApplicationInfo applicationInfo = new()
        {
            SType = StructureType.ApplicationInfo,
            ApiVersion = Vk.Version13
        };

        InstanceCreateInfo createInfo = new()
        {
            SType = StructureType.InstanceCreateInfo,
            PApplicationInfo = &applicationInfo
        };

        var lastExtension = (MinimalExtension*) &createInfo;
        foreach (var extensionPtr in _instanceFeatureExtensions)
        {
            var extension = (MinimalExtension*) extensionPtr;
            extension->PNext = null;
            lastExtension->PNext = extension;
            lastExtension = extension;
        }

        var availableLayers = EnumerateInstanceLayers();
        List<string> instanceLayers = new();

        var validationLayers = GetValidationLayers();

        if (validationLayers is null) _validationLayerOverride = false;
        else instanceLayers.AddRange(validationLayers);

        foreach (var (modName, layer, hardRequirement) in _additionalInstanceLayers)
        {
            if (!availableLayers.Contains(layer))
            {
                if (hardRequirement)
                    Logger.WriteLog(
                        $"Instance layer {layer} is not available. Requested by mod {modName}",
                        LogImportance.Exception, "Render");
                else
                    Logger.WriteLog(
                        $"Optional instance layer {layer} is not available. Requested by mod {modName}",
                        LogImportance.Warning, "Render");
                continue;
            }

            instanceLayers.Add(layer);
        }

        createInfo.EnabledLayerCount = (uint) instanceLayers.Count;
        createInfo.PpEnabledLayerNames = (byte**) SilkMarshal.StringArrayToPtr(instanceLayers);

        var availableInstanceExtensions = new HashSet<string>(EnumerateInstanceExtensions());
        var windowExtensionPtr =
            Engine.Window!.WindowInstance.VkSurface!.GetRequiredExtensions(out var windowExtensionCount);
        var windowExtensions = SilkMarshal.PtrToStringArray((nint) windowExtensionPtr, (int) windowExtensionCount);

        List<string> instanceExtensions = new();

        foreach (var extension in windowExtensions)
        {
            Logger.AssertAndThrow(availableInstanceExtensions.Contains(extension),
                $"The following vulkan extension {extension} is required but not available", "Render");
            instanceExtensions.Add(extension);
        }

        foreach (var (modName, extension, hardRequirement) in _additionalInstanceExtensions)
        {
            if (!availableInstanceExtensions.Contains(extension))
            {
                if (hardRequirement)
                    Logger.WriteLog(
                        $"Extension {extension} is not available. Requested by mod {modName}",
                        LogImportance.Exception, "Render");
                else
                    Logger.WriteLog(
                        $"Optional vulkan extension {extension} is not available. Requested by mod {modName}",
                        LogImportance.Warning, "Render");
                continue;
            }

            instanceExtensions.Add(extension);
        }

        createInfo.EnabledExtensionCount = (uint) instanceExtensions.Count;
        createInfo.PpEnabledExtensionNames = (byte**) SilkMarshal.StringArrayToPtr(instanceExtensions);

        Assert(Vk.CreateInstance(createInfo, AllocationCallback, out var instance));
        Instance = instance;
        Vk.CurrentInstance = Instance;

        SilkMarshal.Free((nint) createInfo.PpEnabledLayerNames);
        SilkMarshal.Free((nint) createInfo.PpEnabledExtensionNames);

        LoadedInstanceLayers = new HashSet<string>(instanceLayers);
        LoadedInstanceExtensions = new HashSet<string>(instanceExtensions);

        foreach (var extension in _instanceFeatureExtensions)
        {
            AllocationHandler.Free(extension);
        }
    }

    private static void Resized(Vector2D<int> obj)
    {
    }

    internal static void CleanupSwapchain()
    {
        AssertVulkanInstance();
        Logger.AssertAndThrow(VkSwapchain is not null, "KhrSwapchain extension is null", "Renderer");


        foreach (var framebuffer in SwapchainFramebuffers)
            Vk.DestroyFramebuffer(Device, framebuffer, AllocationCallback);

        RenderPassHandler.DestroyMainRenderPass();
        foreach (var imageView in SwapchainImageViews) Vk.DestroyImageView(Device, imageView, AllocationCallback);

        Vk.DestroyImageView(Device, DepthImageView, AllocationCallback);
        DepthTexture.Dispose();

        VkSwapchain.DestroySwapchain(Device, Swapchain, AllocationCallback);
    }

    internal static void Shutdown()
    {
        Logger.WriteLog("Shutdown Vulkan", LogImportance.Info, "Render");
        Assert(Vk.DeviceWaitIdle(Device));

        foreach (var fence in _renderFences) Vk.DestroyFence(Device, fence, AllocationCallback);

        Vk.DestroySemaphore(Device, _semaphoreImageAvailable, AllocationCallback);
        Vk.DestroySemaphore(Device, _semaphoreRenderingDone, AllocationCallback);

        Vk.DestroyCommandPool(Device, _singleTimeCommandPool, AllocationCallback);
        foreach (var commandPool in GraphicsCommandPool)
            Vk.DestroyCommandPool(Device, commandPool, AllocationCallback);

        CleanupSwapchain();
        Vk.DestroyDevice(Device, AllocationCallback);
        VkSurface?.DestroySurface(Instance, Surface, AllocationCallback);
        Vk.DestroyInstance(Instance, AllocationCallback);
    }

    /// <summary>
    ///     Wait for the completion of every running gpu process
    /// </summary>
    public static void WaitForAll()
    {
        Assert(Vk.DeviceWaitIdle(Device));
    }

    private static readonly object _singleCbLock = new();

    /// <summary>
    ///     Get a command buffer for single time execution
    /// </summary>
    /// <returns>Single time command buffer</returns>
    public static CommandBuffer GetSingleTimeCommandBuffer()
    {
        CommandBuffer buffer;
        lock (_singleCbLock)
        {
            if (!_singleTimeCommandBuffers.TryDequeue(out buffer))
            {
                CommandBufferAllocateInfo allocateInfo = new()
                {
                    SType = StructureType.CommandBufferAllocateInfo,
                    Level = CommandBufferLevel.Primary,
                    CommandBufferCount = 1,
                    CommandPool = _singleTimeCommandPool
                };
                Assert(Vk.AllocateCommandBuffers(Device, allocateInfo, out buffer));
            }
        }

        Assert(Vk.BeginCommandBuffer(buffer,
            new CommandBufferBeginInfo
            {
                SType = StructureType.CommandBufferBeginInfo,
                Flags = CommandBufferUsageFlags.CommandBufferUsageOneTimeSubmitBit
            }));
        return buffer;
    }

    /// <summary>
    ///     Execute a pre fetched single time command buffer
    /// </summary>
    /// <param name="buffer"></param>
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
            PCommandBuffers = &buffer
        };
        lock (_singleCbLock)
        {
            Assert(Vk.QueueSubmit(GraphicQueue, 1, submitInfo, fence));
            Vk.WaitForFences(Device, 1, in fence, Vk.True, ulong.MaxValue);
            Vk.ResetCommandBuffer(buffer, 0);
            Vk.DestroyFence(Device, fence, AllocationCallback);
            _singleTimeCommandBuffers.Enqueue(buffer);
        }
    }

    /// <summary>
    ///     Clear the color texture
    /// </summary>
    /// <param name="texture">The texture to clear</param>
    /// <param name="clearColorValue">The clear value</param>
    public static void ClearColorTexture(Texture texture, ClearColorValue clearColorValue)
    {
        var layers = texture.ArrayLayers;
        if ((texture.Usage & TextureUsage.Cubemap) != 0) layers *= 6;

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

    /// <summary>
    ///     Clear a depth texture
    /// </summary>
    /// <param name="texture">Texture to clear</param>
    /// <param name="clearDepthStencilValue">Clear value</param>
    public static void ClearDepthTexture(Texture texture, ClearDepthStencilValue clearDepthStencilValue)
    {
        var effectiveLayers = texture.ArrayLayers;
        if ((texture.Usage & TextureUsage.Cubemap) != 0) effectiveLayers *= 6;

        var aspect = FormatHelpers.IsStencilFormat(texture.Format)
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

    /// <summary>
    ///     Transition the layout of the texture
    /// </summary>
    /// <param name="texture">Texture to transition</param>
    /// <param name="layout">New layout for the texture</param>
    public static void TransitionImageLayout(Texture texture, ImageLayout layout)
    {
        var cb = GetSingleTimeCommandBuffer();
        texture.TransitionImageLayout(cb, 0, texture.MipLevels, 0, texture.ArrayLayers, layout);
        ExecuteSingleTimeCommandBuffer(cb);
    }
}