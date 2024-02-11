using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using JetBrains.Annotations;
using MintyCore.Graphics.Managers;
using MintyCore.Graphics.Utils;
using MintyCore.Graphics.VulkanObjects;
using MintyCore.Identifications;
using MintyCore.Registries;
using MintyCore.Utils;
using Serilog;
using Serilog.Events;
using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using VkSemaphore = Silk.NET.Vulkan.Semaphore;
using static MintyCore.Graphics.Utils.VulkanUtils;

namespace MintyCore.Graphics.Implementations;

/// <summary>
///     Base class to interact with the VulkanAPI through the Silk.Net Library
///     <remarks>You wont find a documentation how to use vulkan here</remarks>
/// </summary>
[PublicAPI]
[Singleton<IVulkanEngine>(SingletonContextFlags.NoHeadless)]
public unsafe class VulkanEngine : IVulkanEngine
{
    private bool _validationLayerOverride = true;
    public bool ValidationLayersActive => Engine.TestingModeActive && _validationLayerOverride;
    private bool _logCallbackActive;
    private DebugUtilsMessengerEXT _debugUtilsMessenger;

    public required IAllocationHandler AllocationHandler { init; private get; }
    public required ITextureManager TextureManager { init; private get; }
    public required IRenderPassManager RenderPassManager { init; get; }
    public required ICommandPoolFactory CommandPoolFactory { init; private get; }
    public required IFenceFactory FenceFactory { init; private get; }


    /// <summary>
    ///     Access point to the vulkan api
    /// </summary>
    public Vk Vk { get; } = Vk.GetApi();

    /// <summary>
    ///     The current vulkan instance
    /// </summary>
    public Instance Instance { get; private set; }

    /// <summary>
    ///     The current vulkan surface
    /// </summary>
    public SurfaceKHR Surface { get; private set; }

    /// <summary>
    ///     The current vulkan physical device
    /// </summary>
    public PhysicalDevice PhysicalDevice { get; private set; }

    /// <summary>
    ///     The current vulkan physical device memory properties
    /// </summary>
    public PhysicalDeviceMemoryProperties PhysicalDeviceMemoryProperties { get; private set; }

    /// <summary>
    ///     The vulkan logical device
    /// </summary>
    public Device Device { get; private set; }

    /// <summary>
    ///     The information about queue family indices
    /// </summary>
    public QueueFamilyIndexes QueueFamilyIndexes { get; private set; }

    /// <summary>
    ///     The vulkan graphics queue
    /// </summary>
    public (Queue queue, object queueLock, uint familyIndex) GraphicQueue { get; private set; }

    /// <summary>
    ///     The vulkan present queue (possible the same as <see cref="GraphicQueue" />)
    /// </summary>
    public (Queue queue, object queueLock, uint familyIndex) PresentQueue { get; private set; }

    /// <summary>
    ///     The vulkan compute queue
    /// </summary>
    public (Queue graphicQueue, object queueLock, uint familyIndex) ComputeQueue { get; private set; }

    /// <summary>
    ///     The vulkan surface api access
    /// </summary>
    // ReSharper disable once NotNullMemberIsNotInitialized
    public KhrSurface? VkSurface { get; private set; }

    /// <summary>
    ///     The vulkan swapchain api access
    /// </summary>
    // ReSharper disable once NotNullMemberIsNotInitialized
    public KhrSwapchain? VkSwapchain { get; private set; }

    /// <summary>
    ///     The vulkan swapchain
    /// </summary>
    public SwapchainKHR Swapchain { get; private set; }

    /// <summary>
    ///     The vulkan swapchain images
    /// </summary>
    public Image[] SwapchainImages { get; private set; } = Array.Empty<Image>();

    /// <summary>
    ///     The swapchain image format
    /// </summary>
    public Format SwapchainImageFormat { get; private set; }

    /// <summary>
    ///     The swapchain extent (size)
    /// </summary>
    public Extent2D SwapchainExtent { get; private set; }

    /// <summary>
    ///     The swapchain image views
    /// </summary>
    public ImageView[] SwapchainImageViews { get; private set; } = Array.Empty<ImageView>();

    public Framebuffer[] SwapchainFramebuffers { get; set; } = Array.Empty<Framebuffer>();

    /// <summary>
    ///     The swapchain image count.
    ///     Useful if you want to have per frame data on the gpu (like dynamic data)
    /// </summary>
    public int SwapchainImageCount => SwapchainImages.Length;

    /// <summary>
    ///     Command pools  for graphic commands
    /// </summary>
    public ManagedCommandPool[] GraphicsCommandPool { get; private set; } = Array.Empty<ManagedCommandPool>();

    /// <summary>
    ///     Command pool for single time command buffers
    /// </summary>
    private ConcurrentDictionary<Thread, ManagedCommandPool> _singleTimeCommandPools = new();

    /// <summary>
    ///     A queue of allocated single time command buffers
    /// </summary>
    private readonly ConcurrentDictionary<Thread, Queue<ManagedCommandBuffer>> _singleTimeCommandBuffersPerThread =
        new();

    private VkSemaphore _semaphoreImageAvailable;
    private VkSemaphore _semaphoreRenderingDone;
    private ManagedFence[] _renderFences = Array.Empty<ManagedFence>();

    /// <summary>
    ///     Whether or not drawing is enabled
    /// </summary>
    public bool DrawEnable { get; private set; }

    private readonly Thread _mainThread = Thread.CurrentThread;

    public void Setup()
    {
        CreateInstance();
        CreateSurface();
        CreateDevice();

        CreateSwapchain();
        CreateSwapchainImageViews();
        CreateCommandPool();

        CreateRenderSemaphore();
        CreateRenderFence();

        Engine.Window!.WindowInstance.FramebufferResize += Resized;
    }


    private ManagedCommandBuffer[] _graphicsMainCommandBuffer = Array.Empty<ManagedCommandBuffer>();

    private Queue<ManagedCommandBuffer>[] _availableGraphicsSecondaryCommandBufferPool =
        Array.Empty<Queue<ManagedCommandBuffer>>();

    private Queue<ManagedCommandBuffer>[] _usedGraphicsSecondaryCommandBufferPool =
        Array.Empty<Queue<ManagedCommandBuffer>>();

    /// <summary>
    ///     The current Image index
    /// </summary>
    public uint ImageIndex { get; private set; }

    /// <inheritdoc />
    public bool PrepareDraw()
    {
        AssertVulkanInstance();
        if (VkSwapchain is null)
            throw new MintyCoreException("KhrSwapchain extension is null");

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

            switch (acquireResult)
            {
                case Result.SuboptimalKhr or Result.ErrorOutOfDateKhr:
                    RecreateSwapchain();
                    continue;
                case < 0:
                    Assert(acquireResult);
                    break;
            }
        } while (acquireResult != Result.Success);

        _renderFences[ImageIndex].Wait();
        _renderFences[ImageIndex].Reset();

        GraphicsCommandPool[ImageIndex].Reset(true);

        while (_usedGraphicsSecondaryCommandBufferPool[ImageIndex].TryDequeue(out var buffer))
            _availableGraphicsSecondaryCommandBufferPool[ImageIndex].Enqueue(buffer);

        _graphicsMainCommandBuffer[ImageIndex].BeginCommandBuffer(CommandBufferUsageFlags.OneTimeSubmitBit);

        DrawEnable = true;
        return true;
    }

    /// <summary>
    ///     Get secondary command buffer for rendering
    ///     CommandBuffers acquired with this method are only valid for the current frame and be returned to the internal pool
    /// </summary>
    /// <returns>Secondary command buffer</returns>
    public ManagedCommandBuffer GetSecondaryCommandBuffer()
    {
        AssertVulkanInstance();

        if (Thread.CurrentThread != _mainThread)
            throw new MintyCoreException("Tried to get secondary command buffer from a multi threaded context");

        if (!DrawEnable)
            throw new MintyCoreException("Tried to create secondary command buffer, while drawing is disabled");

        if (!_availableGraphicsSecondaryCommandBufferPool[ImageIndex].TryDequeue(out var buffer))
            buffer = GraphicsCommandPool[ImageIndex].AllocateCommandBuffer(CommandBufferLevel.Secondary);

        _usedGraphicsSecondaryCommandBufferPool[ImageIndex].Enqueue(buffer);

        return buffer;
    }

    /// <inheritdoc />
    public ManagedCommandBuffer GetRenderCommandBuffer()
    {
        AssertVulkanInstance();

        return _graphicsMainCommandBuffer[ImageIndex];
    }

    /// <summary>
    ///     Execute a secondary command buffer on the graphics command buffer
    /// </summary>
    /// <param name="buffer">Command buffer to execute</param>
    public void ExecuteSecondary(ManagedCommandBuffer buffer)
    {
        AssertVulkanInstance();
        if (Thread.CurrentThread != _mainThread)
            throw new MintyCoreException(
                "Secondary command buffers can only be executed in the main command buffer from the main thread" +
                ", to ensure proper synchronization");

        _graphicsMainCommandBuffer[ImageIndex].ExecuteSecondary(buffer);
    }

    private readonly ConcurrentBag<VkSemaphore> _submitWaitSemaphores = new();
    private readonly ConcurrentBag<PipelineStageFlags> _submitWaitStages = new();

    private readonly ConcurrentBag<VkSemaphore> _submitSignalSemaphores = new();

    private ConcurrentBag<IntPtr> _submitPNexts = new();

    /// <inheritdoc />
    public void AddSubmitWaitSemaphore(VkSemaphore semaphore, PipelineStageFlags waitStage)
    {
        _submitWaitSemaphores.Add(semaphore);
        _submitWaitStages.Add(waitStage);
    }

    /// <inheritdoc />
    public void AddSubmitPNext(IntPtr pNext)
    {
        _submitPNexts.Add(pNext);
    }

    /// <inheritdoc />
    public void AddSubmitSignalSemaphore(VkSemaphore semaphore)
    {
        _submitSignalSemaphores.Add(semaphore);
    }

    /// <summary>
    ///     End the draw of the current frame
    /// </summary>
    public void EndDraw()
    {
        AssertVulkanInstance();
        if (VkSwapchain is null)
            throw new MintyCoreException("KhrSwapchain extension is null");

        DrawEnable = false;

        _graphicsMainCommandBuffer[ImageIndex].EndCommandBuffer();

        var imageAvailable = _semaphoreImageAvailable;
        var renderingDone = _semaphoreRenderingDone;

        var waitSemaphoreCopy = _submitWaitSemaphores.ToArray();
        _submitWaitSemaphores.Clear();
        var waitSemaphoreSpan = (stackalloc VkSemaphore[waitSemaphoreCopy.Length + 1]);
        waitSemaphoreSpan[0] = imageAvailable;
        waitSemaphoreCopy.AsSpan().CopyTo(waitSemaphoreSpan.Slice(1));

        var waitStageCopy = _submitWaitStages.ToArray();
        _submitWaitStages.Clear();
        var waitStageSpan = (stackalloc PipelineStageFlags[waitStageCopy.Length + 1]);
        waitStageSpan[0] = PipelineStageFlags.ColorAttachmentOutputBit;
        waitStageCopy.AsSpan().CopyTo(waitStageSpan.Slice(1));

        var signalSemaphoreCopy = _submitSignalSemaphores.ToArray();
        _submitSignalSemaphores.Clear();
        var signalSemaphoreSpan = (stackalloc VkSemaphore[signalSemaphoreCopy.Length + 1]);
        signalSemaphoreSpan[0] = renderingDone;
        signalSemaphoreCopy.AsSpan().CopyTo(signalSemaphoreSpan.Slice(1));

        var pNextCopy = _submitPNexts.ToArray();
        _submitPNexts.Clear();
        var pNextSpan = (stackalloc IntPtr[pNextCopy.Length]);
        pNextCopy.AsSpan().CopyTo(pNextSpan);

        //TODO write a global pNext implementation/support
        for (int i = 0; i < pNextSpan.Length; i++)
        {
            if (i + 1 < pNextSpan.Length)
            {
                var minimal = (MinimalExtension*)pNextSpan[i];
                minimal->PNext = pNextSpan[i + 1].ToPointer();
            }
        }

        var buffer = _graphicsMainCommandBuffer[ImageIndex].InternalCommandBuffer;
        SubmitInfo submitInfo = new()
        {
            SType = StructureType.SubmitInfo,
            PNext = pNextSpan.Length != 0 ? pNextSpan[0].ToPointer() : null,
            CommandBufferCount = 1,
            PCommandBuffers = &buffer,
            WaitSemaphoreCount = (uint)waitSemaphoreSpan.Length,
            SignalSemaphoreCount = (uint)signalSemaphoreSpan.Length,
            PWaitSemaphores = (VkSemaphore*)Unsafe.AsPointer(ref waitSemaphoreSpan.GetPinnableReference()),
            PSignalSemaphores = (VkSemaphore*)Unsafe.AsPointer(ref signalSemaphoreSpan.GetPinnableReference()),
            PWaitDstStageMask = (PipelineStageFlags*)Unsafe.AsPointer(ref waitStageSpan.GetPinnableReference())
        };

        lock (GraphicQueue.queueLock)
            Assert(Vk.QueueSubmit(GraphicQueue.queue, 1u, submitInfo, _renderFences[ImageIndex].Fence));

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

        lock (PresentQueue.queueLock)
            VkSwapchain.QueuePresent(PresentQueue.queue, presentInfo);
    }

    private void CreateRenderFence()
    {
        AssertVulkanInstance();

        _renderFences = new ManagedFence[SwapchainImageCount];
        for (var i = 0; i < SwapchainImageCount; i++)
            _renderFences[i] = FenceFactory.CreateFence(FenceCreateFlags.SignaledBit);
    }

    private void CreateRenderSemaphore()
    {
        AssertVulkanInstance();

        SemaphoreCreateInfo semaphoreCreateInfo = new()
        {
            SType = StructureType.SemaphoreCreateInfo
        };

        Assert(Vk.CreateSemaphore(Device, semaphoreCreateInfo, null,
            out _semaphoreImageAvailable));
        Assert(Vk.CreateSemaphore(Device, semaphoreCreateInfo, null,
            out _semaphoreRenderingDone));
    }

    private void CreateCommandPool()
    {
        AssertVulkanInstance();

        GraphicsCommandPool = new ManagedCommandPool[SwapchainImageCount];

        var commandPoolDescription = new CommandPoolDescription(QueueFamilyIndexes.GraphicsFamily!.Value);

        for (var i = 0; i < SwapchainImageCount; i++)
            GraphicsCommandPool[i] = CommandPoolFactory.CreateCommandPool(commandPoolDescription);

        _availableGraphicsSecondaryCommandBufferPool = new Queue<ManagedCommandBuffer>[SwapchainImageCount];
        _usedGraphicsSecondaryCommandBufferPool = new Queue<ManagedCommandBuffer>[SwapchainImageCount];

        _graphicsMainCommandBuffer = new ManagedCommandBuffer[SwapchainImageCount];

        for (var i = 0; i < SwapchainImageCount; i++)
        {
            _graphicsMainCommandBuffer[i] = GraphicsCommandPool[i].AllocateCommandBuffer(CommandBufferLevel.Primary);

            _availableGraphicsSecondaryCommandBufferPool[i] = new Queue<ManagedCommandBuffer>();
            _usedGraphicsSecondaryCommandBufferPool[i] = new Queue<ManagedCommandBuffer>();
        }
    }

    private void CreateSwapchainImageViews()
    {
        AssertVulkanInstance();

        SwapchainImageViews = new ImageView[SwapchainImageCount];

        for (var i = 0; i < SwapchainImageCount; i++)
        {
            ImageViewCreateInfo createInfo = new()
            {
                SType = StructureType.ImageViewCreateInfo,
                Image = SwapchainImages[i],
                ViewType = ImageViewType.Type2D,
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
                    AspectMask = ImageAspectFlags.ColorBit,
                    LayerCount = 1,
                    LevelCount = 1,
                    BaseArrayLayer = 0,
                    BaseMipLevel = 0
                }
            };

            Assert(Vk.CreateImageView(Device, createInfo, null, out SwapchainImageViews[i]));
        }
    }

    private void CreateSwapchain()
    {
        AssertVulkanInstance();

        Log.Debug("Creating swapchain");

        var result = TryGetSwapChainSupport(out var support);
        if (!result)
            throw new MintyCoreException("Failed to get swapchain support information's");

        //Deconstruct the tuple into the single values
        var (capabilities, formats, presentModes) = support;

        var format = formats.FirstOrDefault(x => x.Format == Format.B8G8R8A8Unorm, formats[0]);

        var presentMode = presentModes.Contains(PresentModeKHR.MailboxKhr)
            ? PresentModeKHR.MailboxKhr
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
            ImageUsage = ImageUsageFlags.ColorAttachmentBit | ImageUsageFlags.InputAttachmentBit |
                         ImageUsageFlags.TransferDstBit,
            Surface = Surface,
            PreTransform = capabilities.CurrentTransform,
            CompositeAlpha = CompositeAlphaFlagsKHR.OpaqueBitKhr,
            Clipped = Vk.True,
            OldSwapchain = default,
            MinImageCount = imageCount
        };

        var indices = QueueFamilyIndexes;
        var queueFamilyIndices = stackalloc uint[2]
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

        if (!Vk.TryGetDeviceExtension(Instance, Device, out KhrSwapchain khrSwapchain))
            throw new MintyCoreException("KhrSwapchain extension not found");

        VkSwapchain = khrSwapchain;

        Assert(VkSwapchain.CreateSwapchain(Device, createInfo, null, out var swapchain));
        Swapchain = swapchain;

        VkSwapchain.GetSwapchainImages(Device, Swapchain, ref imageCount, null);
        SwapchainImages = new Image[imageCount];
        VkSwapchain.GetSwapchainImages(Device, Swapchain, ref imageCount, out SwapchainImages[0]);

        SwapchainImageFormat = format.Format;
        SwapchainExtent = extent;
    }

    public void RecreateSwapchain()
    {
        AssertVulkanInstance();

        Vk.DeviceWaitIdle(Device);
        CleanupSwapchain();

        CreateSwapchain();
        CreateSwapchainImageViews();

        Vk.DestroySemaphore(Device, _semaphoreImageAvailable, null);
        SemaphoreCreateInfo createInfo = new()
        {
            SType = StructureType.SemaphoreCreateInfo
        };
        Assert(Vk.CreateSemaphore(Device, createInfo, null, out _semaphoreImageAvailable));
    }

    private Extent2D GetSwapChainExtent(SurfaceCapabilitiesKHR swapchainSupportCapabilities)
    {
        AssertVulkanInstance();
        if (swapchainSupportCapabilities.CurrentExtent.Width != uint.MaxValue)
            return swapchainSupportCapabilities.CurrentExtent;

        if (Engine.Window is null)
            return default;

        var actualExtent = new Extent2D
        {
            Height = (uint)Engine.Window.WindowInstance.FramebufferSize.Y,
            Width = (uint)Engine.Window.WindowInstance.FramebufferSize.X
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

    private bool TryGetSwapChainSupport(
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

    private readonly List<(string modName, string extensionName, bool hardRequirement)> _deviceExtensions = new()
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
    public void AddDeviceExtension(string modName, string extensionName, bool hardRequirement)
    {
        _deviceExtensions.Add((modName, extensionName, hardRequirement));
    }

    /// <summary>
    /// List of loaded device extensions
    /// </summary>
    public IReadOnlySet<string> LoadedDeviceExtensions { get; private set; } = new HashSet<string>();

    private readonly List<IntPtr> _deviceFeatureExtensions = new();

    /// <summary>
    ///  Add a device feature extension
    /// </summary>
    /// <param name="extension"> The extension to add</param>
    /// <typeparam name="TExtension"> The type of the extension</typeparam>
    public void AddDeviceFeatureExension<TExtension>(TExtension extension)
        where TExtension : unmanaged, IChainable
    {
        var copiedExtension = (TExtension*)AllocationHandler.Malloc<TExtension>();
        *copiedExtension = extension;
        _deviceFeatureExtensions.Add((IntPtr)copiedExtension);
    }

    /// <summary>
    /// Event called right before the device is created
    /// Remember to unsubscribe to don't break mod unloading
    /// </summary>
    public event Action OnDeviceCreation = delegate { };

    private void CreateDevice()
    {
        OnDeviceCreation();
        Log.Debug("Creating device");
        PhysicalDevice = ChoosePhysicalDevice(EnumerateDevices(Instance));


        Vk.GetPhysicalDeviceMemoryProperties(PhysicalDevice, out var memoryProperties);
        PhysicalDeviceMemoryProperties = memoryProperties;

        QueueFamilyIndexes = GetQueueFamilyIndexes(PhysicalDevice);

        var queueCount = QueueFamilyIndexes.GraphicsFamily!.Value != QueueFamilyIndexes.ComputeFamily!.Value
            ? 2u
            : 1u;
        if (QueueFamilyIndexes.GraphicsFamily!.Value != QueueFamilyIndexes.PresentFamily!.Value) queueCount++;
        var queueCreateInfo = stackalloc DeviceQueueCreateInfo[(int)queueCount];
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
            SamplerAnisotropy = Vk.True,
            FragmentStoresAndAtomics = Vk.True
        };

        DeviceCreateInfo deviceCreateInfo = new()
        {
            SType = StructureType.DeviceCreateInfo,
            PQueueCreateInfos = queueCreateInfo,
            QueueCreateInfoCount = queueCount,
            PEnabledFeatures = &enabledFeatures
        };

        var lastExtension = (MinimalExtension*)&deviceCreateInfo;
        foreach (var extension in _deviceFeatureExtensions)
        {
            var extensionPtr = (MinimalExtension*)extension;
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
                    throw new MintyCoreException(
                        $"Device extension {extension} is not available. Requested by mod {modName}");
                }
                else
                {
                    Log.Warning("Optional device extension {Extension} is not available. Requested by mod {ModName}",
                        extension, modName);
                }

                continue;
            }

            extensions.Add(extension);
        }

        deviceCreateInfo.EnabledExtensionCount = (uint)extensions.Count;
        deviceCreateInfo.PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr(extensions);

        Assert(Vk.CreateDevice(PhysicalDevice, deviceCreateInfo, null, out var device));
        Device = device;
        SilkMarshal.Free((nint)deviceCreateInfo.PpEnabledExtensionNames);

        Vk.GetDeviceQueue(Device, QueueFamilyIndexes.GraphicsFamily.Value, 0, out var graphicQueue);
        Vk.GetDeviceQueue(Device, QueueFamilyIndexes.ComputeFamily.Value, 0, out var computeQueue);
        Vk.GetDeviceQueue(Device, QueueFamilyIndexes.PresentFamily.Value, 0, out var presentQueue);

        GraphicQueue = (graphicQueue, new object(), QueueFamilyIndexes.GraphicsFamily.Value);
        ComputeQueue = (computeQueue, new object(), QueueFamilyIndexes.ComputeFamily.Value);
        PresentQueue = presentQueue.Handle != graphicQueue.Handle
            ? (presentQueue, new object(), QueueFamilyIndexes.PresentFamily.Value)
            : GraphicQueue;

        LoadedDeviceExtensions = new HashSet<string>(extensions);

        foreach (var intPtr in _deviceFeatureExtensions)
        {
            AllocationHandler.Free(intPtr);
        }
    }

    private QueueFamilyIndexes GetQueueFamilyIndexes(PhysicalDevice device)
    {
        if (VkSurface is null)
            throw new MintyCoreException("KhrSurface extension is null");

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

            if (queueFamily.QueueFlags.HasFlag(QueueFlags.GraphicsBit))
                indexes.GraphicsFamily = i;

            if (queueFamily.QueueFlags.HasFlag(QueueFlags.ComputeBit))
                indexes.ComputeFamily = i;

            VkSurface.GetPhysicalDeviceSurfaceSupport(device, i, Surface, out var presentSupport);

            if (presentSupport == Vk.True) indexes.PresentFamily = i;
        }


        return indexes;
    }

    private PhysicalDevice ChoosePhysicalDevice(IReadOnlyList<PhysicalDevice> devices)
    {
        if (devices.Count == 0)
            throw new MintyCoreException("No graphic device found");
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

    private void CreateSurface()
    {
        Log.Debug("Creating surface");

        if (!Vk.TryGetInstanceExtension(Instance, out KhrSurface vkSurface))
            throw new MintyCoreException("KHR_surface extension not found.");
        VkSurface = vkSurface;

        Surface = Engine.Window!.WindowInstance.VkSurface!.Create(Instance.ToHandle(), (AllocationCallbacks*)null)
            .ToSurface();
    }

    private string[]? GetValidationLayers()
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
                "VK_LAYER_GOOGLE_unique_objects"
            }
        };

        var availableLayersName = EnumerateInstanceLayers();

        return validationLayerNamesPriorityList.FirstOrDefault(validationSet =>
            validationSet.All(validationName => availableLayersName.Contains(validationName)));
    }

    private readonly List<(string requestingMod, string layer, bool hardRequirement)>
        _additionalInstanceLayers =
            new();

    private readonly List<(string requestingMod, string extensions, bool hardRequirement)>
        _additionalInstanceExtensions = new();

    /// <summary>
    /// Add a layer to be used by the instance.
    /// </summary>
    /// <param name="modName"> The name of the mod requesting the layer.</param>
    /// <param name="layers"> The name of the layer.</param>
    /// <param name="hardRequirement"> Whether the layer is a hard requirement. If yes a exception will be thrown if the layer is not available.</param>
    public void AddInstanceLayer(string modName, string layers, bool hardRequirement = true)
    {
        _additionalInstanceLayers.Add((modName, layers, hardRequirement));
    }

    /// <summary>
    /// Add an extension to be used by the instance.
    /// </summary>
    /// <param name="modName"> The name of the mod requesting the extension.</param>
    /// <param name="extensions"> The name of the extension.</param>
    /// <param name="hardRequirement"> Whether the extension is a hard requirement. If yes a exception will be thrown if the extension is not available.</param>
    public void AddInstanceExtension(string modName, string extensions, bool hardRequirement = true)
    {
        _additionalInstanceExtensions.Add((modName, extensions, hardRequirement));
    }

    /// <summary>
    /// List of all loaded instance layers.
    /// </summary>
    public IReadOnlySet<string> LoadedInstanceLayers { get; private set; } = new HashSet<string>();

    /// <summary>
    /// List of all loaded instance extensions.
    /// </summary>
    public IReadOnlySet<string> LoadedInstanceExtensions { get; private set; } = new HashSet<string>();

    private struct MinimalExtension
    {
        [UsedImplicitly] public StructureType SType;
        [UsedImplicitly] public void* PNext;
    }

    private readonly List<IntPtr> _instanceFeatureExtensions = new();

    /// <summary>
    /// Add a instance feature extension
    /// </summary>
    /// <param name="extension">The extension to use</param>
    /// <typeparam name="TExtension">The type of the extension</typeparam>
    public void AddInstanceFeatureExtension<TExtension>(TExtension extension)
        where TExtension : unmanaged, IChainable
    {
        var copiedExtension = (TExtension*)AllocationHandler.Malloc<TExtension>();
        *copiedExtension = extension;
        _instanceFeatureExtensions.Add((IntPtr)copiedExtension);
    }

    private void CreateInstance()
    {
        Log.Debug("Creating instance");
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

        var lastExtension = (MinimalExtension*)&createInfo;
        foreach (var extensionPtr in _instanceFeatureExtensions)
        {
            var extension = (MinimalExtension*)extensionPtr;
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
                    throw new MintyCoreException(
                        $"Instance layer {layer} is not available. Requested by mod {modName}");
                else
                    Log.Warning("Optional instance layer {Layer} is not available. Requested by mod {ModName}",
                        layer, modName);
                continue;
            }

            instanceLayers.Add(layer);
        }

        createInfo.EnabledLayerCount = (uint)instanceLayers.Count;
        createInfo.PpEnabledLayerNames = (byte**)SilkMarshal.StringArrayToPtr(instanceLayers);

        var availableInstanceExtensions = new HashSet<string>(EnumerateInstanceExtensions());
        var windowExtensionPtr =
            Engine.Window!.WindowInstance.VkSurface!.GetRequiredExtensions(out var windowExtensionCount);
        var windowExtensions = SilkMarshal.PtrToStringArray((nint)windowExtensionPtr, (int)windowExtensionCount);

        List<string> instanceExtensions = new();
        
        if (ValidationLayersActive && availableInstanceExtensions.Any(x => x == ExtDebugUtils.ExtensionName))
        {
            _logCallbackActive = true;
            instanceExtensions.Add(ExtDebugUtils.ExtensionName);
        }

        foreach (var extension in windowExtensions)
        {
            if (!availableInstanceExtensions.Contains(extension))
                throw new MintyCoreException(
                    $"The following vulkan extension {extension} is required but not available");
            instanceExtensions.Add(extension);
        }

        foreach (var (modName, extension, hardRequirement) in _additionalInstanceExtensions)
        {
            if (!availableInstanceExtensions.Contains(extension))
            {
                if (hardRequirement)
                    throw new MintyCoreException(
                        $"Instance extension {extension} is not available. Requested by mod {modName}");
                else
                    Log.Warning("Optional vulkan extension {Extension} is not available. Requested by mod {ModName}",
                        extension, modName);
                continue;
            }

            instanceExtensions.Add(extension);
        }

        createInfo.EnabledExtensionCount = (uint)instanceExtensions.Count;
        createInfo.PpEnabledExtensionNames = (byte**)SilkMarshal.StringArrayToPtr(instanceExtensions);

        Assert(Vk.CreateInstance(createInfo, null, out var instance));
        Instance = instance;
        Vk.CurrentInstance = Instance;

        SilkMarshal.Free((nint)createInfo.PpEnabledLayerNames);
        SilkMarshal.Free((nint)createInfo.PpEnabledExtensionNames);

        LoadedInstanceLayers = new HashSet<string>(instanceLayers);
        LoadedInstanceExtensions = new HashSet<string>(instanceExtensions);

        foreach (var extension in _instanceFeatureExtensions)
        {
            AllocationHandler.Free(extension);
        }

        if (!_logCallbackActive) return;

        DebugUtilsMessengerCreateInfoEXT debugUtilsMessengerCreateInfo = new()
        {
            SType = StructureType.DebugUtilsMessengerCreateInfoExt,
            MessageSeverity = DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt |
                              DebugUtilsMessageSeverityFlagsEXT.WarningBitExt |
                              DebugUtilsMessageSeverityFlagsEXT.InfoBitExt |
                              DebugUtilsMessageSeverityFlagsEXT.VerboseBitExt,
            MessageType = DebugUtilsMessageTypeFlagsEXT.GeneralBitExt |
                          DebugUtilsMessageTypeFlagsEXT.ValidationBitExt |
                          DebugUtilsMessageTypeFlagsEXT.PerformanceBitExt,
            PfnUserCallback = new PfnDebugUtilsMessengerCallbackEXT(DebugMessageCallback)
        };

        if (!Vk.TryGetInstanceExtension(Instance, out ExtDebugUtils debugUtils))
            throw new MintyCoreException("DebugUtils extension not found");

        Assert(debugUtils.CreateDebugUtilsMessenger(Instance, debugUtilsMessengerCreateInfo, null,
            out _debugUtilsMessenger));
    }


    private static uint DebugMessageCallback(DebugUtilsMessageSeverityFlagsEXT severity,
        DebugUtilsMessageTypeFlagsEXT messageType,
        DebugUtilsMessengerCallbackDataEXT* callBackData, void* userData)
    {
        var message = Marshal.PtrToStringAnsi((nint)callBackData->PMessage);
        var level = severity switch
        {
            >= DebugUtilsMessageSeverityFlagsEXT.ErrorBitExt => LogEventLevel.Error,
            >= DebugUtilsMessageSeverityFlagsEXT.WarningBitExt => LogEventLevel.Warning,
            >= DebugUtilsMessageSeverityFlagsEXT.InfoBitExt => LogEventLevel.Information,
            _ => LogEventLevel.Debug
        };
        var logObjects = new (ObjectType type, ulong handle)[callBackData->ObjectCount];
        for (var i = 0; i < logObjects.Length; i++)
        {
            logObjects[i] = (callBackData->PObjects[i].ObjectType, callBackData->PObjects[i].ObjectHandle);
        }

        Log.Write(level, "Vulkan validation layer produced '{Message}' with following related objects {LogObjects}",
            message, logObjects);

        return Vk.False;
    }

    private void Resized(Vector2D<int> obj)
    {
    }

    public void CleanupSwapchain()
    {
        AssertVulkanInstance();
        if (VkSwapchain is null)
            throw new MintyCoreException("KhrSwapchain extension is null");

        foreach (var imageView in SwapchainImageViews) Vk.DestroyImageView(Device, imageView, null);

        VkSwapchain.DestroySwapchain(Device, Swapchain, null);
    }

    public void Shutdown()
    {
        Log.Information("Shutting down vulkan");
        Assert(Vk.DeviceWaitIdle(Device));

        foreach (var fence in _renderFences) fence.Dispose();

        Vk.DestroySemaphore(Device, _semaphoreImageAvailable, null);
        Vk.DestroySemaphore(Device, _semaphoreRenderingDone, null);

        foreach (var (_, commandPool) in _singleTimeCommandPools)
            commandPool.Dispose();


        foreach (var commandPool in GraphicsCommandPool)
            commandPool.Dispose();

        CleanupSwapchain();
        Vk.DestroyDevice(Device, null);
        VkSurface?.DestroySurface(Instance, Surface, null);

        if (_debugUtilsMessenger.Handle != default &&
            Vk.TryGetInstanceExtension(Instance, out ExtDebugUtils debugUtils))
        {
            debugUtils.DestroyDebugUtilsMessenger(Instance, _debugUtilsMessenger, null);
        }

        Vk.DestroyInstance(Instance, null);
    }

    /// <summary>
    ///     Wait for the completion of every running gpu process
    /// </summary>
    public void WaitForAll()
    {
        Assert(Vk.DeviceWaitIdle(Device));
    }


    /// <summary>
    ///     Get a command buffer for single time execution
    /// </summary>
    /// <returns>Single time command buffer</returns>
    public ManagedCommandBuffer GetSingleTimeCommandBuffer()
    {
        var commandBuffers =
            _singleTimeCommandBuffersPerThread.GetOrAdd(Thread.CurrentThread, _ => new Queue<ManagedCommandBuffer>());

        if (!commandBuffers.TryDequeue(out var buffer))
        {
            if (!_singleTimeCommandPools.TryGetValue(Thread.CurrentThread, out var singleTimeCommandPool))
            {
                var description =
                    new CommandPoolDescription(QueueFamilyIndexes.GraphicsFamily!.Value, IsResettable: true);

                singleTimeCommandPool = CommandPoolFactory.CreateCommandPool(description);

                _singleTimeCommandPools.TryAdd(Thread.CurrentThread, singleTimeCommandPool);
            }

            buffer = singleTimeCommandPool.AllocateCommandBuffer(CommandBufferLevel.Primary);
        }

        buffer.BeginCommandBuffer(CommandBufferUsageFlags.OneTimeSubmitBit);
        return buffer;
    }

    /// <summary>
    ///     Execute a pre fetched single time command buffer
    /// </summary>
    /// <param name="buffer"></param>
    public void ExecuteSingleTimeCommandBuffer(ManagedCommandBuffer buffer)
    {
        FenceCreateInfo fenceCreateInfo = new()
        {
            SType = StructureType.FenceCreateInfo
        };
        Assert(Vk.CreateFence(Device, in fenceCreateInfo, null, out var fence));

        buffer.EndCommandBuffer();

        var nativeBuffer = buffer.InternalCommandBuffer;
        SubmitInfo submitInfo = new()
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &nativeBuffer
        };

        lock (GraphicQueue.queueLock)
            Assert(Vk.QueueSubmit(GraphicQueue.queue, 1, submitInfo, fence));
        Vk.WaitForFences(Device, 1, in fence, Vk.True, ulong.MaxValue);
        buffer.Reset();
        Vk.DestroyFence(Device, fence, null);
        _singleTimeCommandBuffersPerThread.GetOrAdd(Thread.CurrentThread, _ => new Queue<ManagedCommandBuffer>())
            .Enqueue(buffer);
    }

    /// <summary>
    ///     Clear the color texture
    /// </summary>
    /// <param name="texture">The texture to clear</param>
    /// <param name="clearColorValue">The clear value</param>
    public void ClearColorTexture(Texture texture, ClearColorValue clearColorValue)
    {
        var layers = texture.ArrayLayers;
        if ((texture.Usage & TextureUsage.Cubemap) != 0) layers *= 6;

        ImageSubresourceRange subresourceRange = new()
        {
            AspectMask = ImageAspectFlags.ColorBit,
            LayerCount = layers,
            LevelCount = texture.MipLevels,
            BaseArrayLayer = 0,
            BaseMipLevel = 0
        };

        var buffer = GetSingleTimeCommandBuffer();
        texture.TransitionImageLayout(buffer, 0, texture.MipLevels, 0, layers, ImageLayout.TransferDstOptimal);
        buffer.ClearColorImage(texture, clearColorValue, subresourceRange, ImageLayout.TransferDstOptimal);
        texture.TransitionImageLayout(buffer, 0, texture.MipLevels, 0, layers,
            texture.IsSwapchainTexture ? ImageLayout.PresentSrcKhr : ImageLayout.ColorAttachmentOptimal);
        ExecuteSingleTimeCommandBuffer(buffer);
    }

    /// <summary>
    ///     Clear a depth texture
    /// </summary>
    /// <param name="texture">Texture to clear</param>
    /// <param name="clearDepthStencilValue">Clear value</param>
    public void ClearDepthTexture(Texture texture, ClearDepthStencilValue clearDepthStencilValue)
    {
        var effectiveLayers = texture.ArrayLayers;
        if ((texture.Usage & TextureUsage.Cubemap) != 0) effectiveLayers *= 6;

        var aspect = FormatHelpers.IsStencilFormat(texture.Format)
            ? ImageAspectFlags.DepthBit | ImageAspectFlags.StencilBit
            : ImageAspectFlags.DepthBit;

        ImageSubresourceRange range = new(
            aspect,
            0,
            texture.MipLevels,
            0,
            effectiveLayers);

        var cb = GetSingleTimeCommandBuffer();

        texture.TransitionImageLayout(cb, 0, texture.MipLevels, 0, effectiveLayers, ImageLayout.TransferDstOptimal);
        cb.ClearDepthStencilImage(texture, clearDepthStencilValue, range, ImageLayout.TransferDstOptimal);
        texture.TransitionImageLayout(cb, 0, texture.MipLevels, 0, effectiveLayers,
            ImageLayout.DepthStencilAttachmentOptimal);
        ExecuteSingleTimeCommandBuffer(cb);
    }

    /// <summary>
    ///     Transition the layout of the texture
    /// </summary>
    /// <param name="texture">Texture to transition</param>
    /// <param name="layout">New layout for the texture</param>
    public void TransitionImageLayout(Texture texture, ImageLayout layout)
    {
        var cb = GetSingleTimeCommandBuffer();
        texture.TransitionImageLayout(cb, 0, texture.MipLevels, 0, texture.ArrayLayers, layout);
        ExecuteSingleTimeCommandBuffer(cb);
    }

    /// <summary>
    /// </summary>
    /// <param name="cb"></param>
    /// <param name="image"></param>
    /// <param name="baseMipLevel"></param>
    /// <param name="levelCount"></param>
    /// <param name="baseArrayLayer"></param>
    /// <param name="layerCount"></param>
    /// <param name="aspectMask"></param>
    /// <param name="oldLayout"></param>
    /// <param name="newLayout"></param>
    public void TransitionImageLayout(
        ManagedCommandBuffer cb,
        Image image,
        uint baseMipLevel,
        uint levelCount,
        uint baseArrayLayer,
        uint layerCount,
        ImageAspectFlags aspectMask,
        ImageLayout oldLayout,
        ImageLayout newLayout)
    {
        Debug.Assert(oldLayout != newLayout);
        ImageMemoryBarrier barrier = new()
        {
            SType = StructureType.ImageMemoryBarrier,
            OldLayout = oldLayout,
            NewLayout = newLayout,
            SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
            DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
            Image = image,
            SubresourceRange =
            {
                AspectMask = aspectMask, BaseMipLevel = baseMipLevel,
                LevelCount = levelCount,
                BaseArrayLayer = baseArrayLayer,
                LayerCount = layerCount
            }
        };

        PipelineStageFlags srcStageFlags = 0;
        PipelineStageFlags dstStageFlags = 0;

        switch (oldLayout)
        {
            case ImageLayout.Undefined or ImageLayout.Preinitialized when newLayout == ImageLayout.TransferDstOptimal:
                barrier.SrcAccessMask = 0;
                barrier.DstAccessMask = AccessFlags.TransferWriteBit;
                srcStageFlags = PipelineStageFlags.TopOfPipeBit;
                dstStageFlags = PipelineStageFlags.TransferBit;
                break;
            case ImageLayout.ShaderReadOnlyOptimal when newLayout == ImageLayout.TransferSrcOptimal:
                barrier.SrcAccessMask = AccessFlags.ShaderReadBit;
                barrier.DstAccessMask = AccessFlags.TransferReadBit;
                srcStageFlags = PipelineStageFlags.FragmentShaderBit;
                dstStageFlags = PipelineStageFlags.TransferBit;
                break;
            case ImageLayout.ShaderReadOnlyOptimal when newLayout == ImageLayout.TransferDstOptimal:
                barrier.SrcAccessMask = AccessFlags.ShaderReadBit;
                barrier.DstAccessMask = AccessFlags.TransferWriteBit;
                srcStageFlags = PipelineStageFlags.FragmentShaderBit;
                dstStageFlags = PipelineStageFlags.TransferBit;
                break;
            case ImageLayout.Preinitialized when newLayout == ImageLayout.TransferSrcOptimal:
                barrier.SrcAccessMask = 0;
                barrier.DstAccessMask = AccessFlags.TransferReadBit;
                srcStageFlags = PipelineStageFlags.TopOfPipeBit;
                dstStageFlags = PipelineStageFlags.TransferBit;
                break;
            case ImageLayout.Preinitialized when newLayout == ImageLayout.General:
                barrier.SrcAccessMask = 0;
                barrier.DstAccessMask = AccessFlags.ShaderReadBit;
                srcStageFlags = PipelineStageFlags.TopOfPipeBit;
                dstStageFlags = PipelineStageFlags.ComputeShaderBit;
                break;
            case ImageLayout.Preinitialized when newLayout == ImageLayout.ShaderReadOnlyOptimal:
                barrier.SrcAccessMask = 0;
                barrier.DstAccessMask = AccessFlags.ShaderReadBit;
                srcStageFlags = PipelineStageFlags.TopOfPipeBit;
                dstStageFlags = PipelineStageFlags.FragmentShaderBit;
                break;
            case ImageLayout.General when newLayout == ImageLayout.ShaderReadOnlyOptimal:
                barrier.SrcAccessMask = AccessFlags.TransferReadBit;
                barrier.DstAccessMask = AccessFlags.ShaderReadBit;
                srcStageFlags = PipelineStageFlags.TransferBit;
                dstStageFlags = PipelineStageFlags.FragmentShaderBit;
                break;
            case ImageLayout.ShaderReadOnlyOptimal when newLayout == ImageLayout.General:
                barrier.SrcAccessMask = AccessFlags.ShaderReadBit;
                barrier.DstAccessMask = AccessFlags.ShaderReadBit;
                srcStageFlags = PipelineStageFlags.FragmentShaderBit;
                dstStageFlags = PipelineStageFlags.ComputeShaderBit;
                break;
            case ImageLayout.TransferSrcOptimal when newLayout == ImageLayout.ShaderReadOnlyOptimal:
                barrier.SrcAccessMask = AccessFlags.TransferReadBit;
                barrier.DstAccessMask = AccessFlags.ShaderReadBit;
                srcStageFlags = PipelineStageFlags.TransferBit;
                dstStageFlags = PipelineStageFlags.FragmentShaderBit;
                break;
            case ImageLayout.TransferDstOptimal when newLayout == ImageLayout.ShaderReadOnlyOptimal:
                barrier.SrcAccessMask = AccessFlags.TransferWriteBit;
                barrier.DstAccessMask = AccessFlags.ShaderReadBit;
                srcStageFlags = PipelineStageFlags.TransferBit;
                dstStageFlags = PipelineStageFlags.FragmentShaderBit;
                break;
            case ImageLayout.TransferSrcOptimal when newLayout == ImageLayout.TransferDstOptimal:
                barrier.SrcAccessMask = AccessFlags.TransferReadBit;
                barrier.DstAccessMask = AccessFlags.TransferWriteBit;
                srcStageFlags = PipelineStageFlags.TransferBit;
                dstStageFlags = PipelineStageFlags.TransferBit;
                break;
            case ImageLayout.TransferDstOptimal when newLayout == ImageLayout.TransferSrcOptimal:
                barrier.SrcAccessMask = AccessFlags.TransferWriteBit;
                barrier.DstAccessMask = AccessFlags.TransferReadBit;
                srcStageFlags = PipelineStageFlags.TransferBit;
                dstStageFlags = PipelineStageFlags.TransferBit;
                break;
            case ImageLayout.ColorAttachmentOptimal when newLayout == ImageLayout.TransferSrcOptimal:
                barrier.SrcAccessMask = AccessFlags.ColorAttachmentWriteBit;
                barrier.DstAccessMask = AccessFlags.TransferReadBit;
                srcStageFlags = PipelineStageFlags.ColorAttachmentOutputBit;
                dstStageFlags = PipelineStageFlags.TransferBit;
                break;
            case ImageLayout.ColorAttachmentOptimal when newLayout == ImageLayout.TransferDstOptimal:
                barrier.SrcAccessMask = AccessFlags.ColorAttachmentWriteBit;
                barrier.DstAccessMask = AccessFlags.TransferWriteBit;
                srcStageFlags = PipelineStageFlags.ColorAttachmentOutputBit;
                dstStageFlags = PipelineStageFlags.TransferBit;
                break;
            case ImageLayout.ColorAttachmentOptimal when newLayout == ImageLayout.ShaderReadOnlyOptimal:
                barrier.SrcAccessMask = AccessFlags.ColorAttachmentWriteBit;
                barrier.DstAccessMask = AccessFlags.ShaderReadBit;
                srcStageFlags = PipelineStageFlags.ColorAttachmentOutputBit;
                dstStageFlags = PipelineStageFlags.FragmentShaderBit;
                break;
            case ImageLayout.DepthStencilAttachmentOptimal when newLayout == ImageLayout.ShaderReadOnlyOptimal:
                barrier.SrcAccessMask = AccessFlags.DepthStencilAttachmentWriteBit;
                barrier.DstAccessMask = AccessFlags.ShaderReadBit;
                srcStageFlags = PipelineStageFlags.LateFragmentTestsBit;
                dstStageFlags = PipelineStageFlags.FragmentShaderBit;
                break;
            case ImageLayout.ColorAttachmentOptimal when newLayout == ImageLayout.PresentSrcKhr:
                barrier.SrcAccessMask = AccessFlags.ColorAttachmentWriteBit;
                barrier.DstAccessMask = AccessFlags.MemoryReadBit;
                srcStageFlags = PipelineStageFlags.ColorAttachmentOutputBit;
                dstStageFlags = PipelineStageFlags.BottomOfPipeBit;
                break;
            case ImageLayout.TransferDstOptimal when newLayout == ImageLayout.PresentSrcKhr:
                barrier.SrcAccessMask = AccessFlags.TransferWriteBit;
                barrier.DstAccessMask = AccessFlags.MemoryReadBit;
                srcStageFlags = PipelineStageFlags.TransferBit;
                dstStageFlags = PipelineStageFlags.BottomOfPipeBit;
                break;
            case ImageLayout.TransferDstOptimal when newLayout == ImageLayout.ColorAttachmentOptimal:
                barrier.SrcAccessMask = AccessFlags.TransferWriteBit;
                barrier.DstAccessMask = AccessFlags.ColorAttachmentWriteBit;
                srcStageFlags = PipelineStageFlags.TransferBit;
                dstStageFlags = PipelineStageFlags.ColorAttachmentOutputBit;
                break;
            case ImageLayout.TransferDstOptimal when newLayout == ImageLayout.DepthStencilAttachmentOptimal:
                barrier.SrcAccessMask = AccessFlags.TransferWriteBit;
                barrier.DstAccessMask = AccessFlags.DepthStencilAttachmentWriteBit;
                srcStageFlags = PipelineStageFlags.TransferBit;
                dstStageFlags = PipelineStageFlags.LateFragmentTestsBit;
                break;
            default:
                Debug.Fail("Invalid image layout transition.");
                break;
        }

        cb.PipelineBarrier(srcStageFlags, dstStageFlags, 0, new ReadOnlySpan<ImageMemoryBarrier>(&barrier, 1));
    }

    /// <summary>
    ///     Enumerate device extensions
    /// </summary>
    /// <param name="device">Device to enumerate</param>
    /// <param name="layer">Optional to get layer information</param>
    /// <returns>Available extensions</returns>
    public string[] EnumerateDeviceExtensions(PhysicalDevice device, byte* layer = null)
    {
        uint extensionCount = 0;
        Vk.EnumerateDeviceExtensionProperties(device, layer, ref extensionCount, null);
        var properties = new ExtensionProperties[extensionCount];
        Vk.EnumerateDeviceExtensionProperties(device, layer, ref extensionCount, ref properties[0]);
        var extensionNames = new string[extensionCount];
        for (var i = 0; i < extensionCount; i++)
            fixed (byte* name = properties[i].ExtensionName)
            {
                extensionNames[i] = Marshal.PtrToStringAnsi((IntPtr)name) ?? string.Empty;
            }

        return extensionNames;
    }

    /// <summary>
    /// </summary>
    /// <param name="layerName"></param>
    /// <returns></returns>
    public string[] EnumerateInstanceExtensions(byte* layerName = null)
    {
        uint extensionCount = 0;
        Vk.EnumerateInstanceExtensionProperties(layerName, ref extensionCount, null);
        var properties = new ExtensionProperties[extensionCount];
        Vk.EnumerateInstanceExtensionProperties(layerName, ref extensionCount, ref properties[0]);
        var extensionNames = new string[extensionCount];
        for (var i = 0; i < extensionCount; i++)
            fixed (byte* name = properties[i].ExtensionName)
            {
                extensionNames[i] = Marshal.PtrToStringAnsi((IntPtr)name) ?? string.Empty;
            }

        return extensionNames;
    }

    /// <summary>
    /// </summary>
    /// <returns></returns>
    public string[] EnumerateInstanceLayers()
    {
        uint layerCount = 0;
        Vk.EnumerateInstanceLayerProperties(ref layerCount, null);
        var properties = new LayerProperties[layerCount];
        Vk.EnumerateInstanceLayerProperties(ref layerCount, ref properties[0]);
        var layerNames = new string[layerCount];
        for (var i = 0; i < layerCount; i++)
            fixed (byte* name = properties[i].LayerName)
            {
                layerNames[i] = Marshal.PtrToStringAnsi((IntPtr)name) ?? string.Empty;
            }

        return layerNames;
    }

    /// <summary>
    /// </summary>
    /// <param name="instance"></param>
    /// <returns></returns>
    public PhysicalDevice[] EnumerateDevices(Instance instance)
    {
        uint deviceCount = 0;
        Assert(Vk.EnumeratePhysicalDevices(instance, ref deviceCount, null));
        if (deviceCount == 0) return Array.Empty<PhysicalDevice>();

        var devices = new PhysicalDevice[deviceCount];
        Assert(Vk.EnumeratePhysicalDevices(instance, ref deviceCount, ref devices[0]));
        return devices;
    }

    /// <summary>
    /// </summary>
    /// <param name="typeFilter"></param>
    /// <param name="requiredFlags"></param>
    /// <param name="memoryTypeIndex"></param>
    /// <returns></returns>
    public bool FindMemoryType(uint typeFilter, MemoryPropertyFlags requiredFlags, out uint memoryTypeIndex)
    {
        for (var i = 0; i < PhysicalDeviceMemoryProperties.MemoryTypeCount; i++)
        {
            if ((typeFilter & (1u << i)) == 0 ||
                (PhysicalDeviceMemoryProperties.MemoryTypes[i].PropertyFlags & requiredFlags) !=
                requiredFlags) continue;

            memoryTypeIndex = (uint)i;
            return true;
        }

        memoryTypeIndex = uint.MaxValue;
        return false;
    }

    /// <summary>
    /// Check if the vulkan instance is valid
    /// <exception cref="MintyCoreException">No valid vulkan instance is available</exception>
    /// </summary>
    public void AssertVulkanInstance()
    {
        if (Device.Handle == default) throw new MintyCoreException("No valid vulkan instance");
    }
}