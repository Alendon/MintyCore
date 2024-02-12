using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using MintyCore.Graphics.Managers;
using MintyCore.Graphics.Render.Managers;
using MintyCore.Graphics.Utils;
using MintyCore.Graphics.VulkanObjects;
using MintyCore.Utils;
using QuikGraph;
using QuikGraph.Algorithms;
using Serilog;
using Silk.NET.Vulkan;

namespace MintyCore.Graphics.Render;

internal class RenderGraph(
    IInputDataManager inputDataManager,
    IInputModuleManager inputModuleManager,
    IIntermediateDataManager intermediateDataManager,
    IAsyncFenceAwaiter fenceAwaiter,
    IVulkanEngine vulkanEngine,
    IFenceFactory fenceFactory,
    IRenderModuleManager renderModuleManager,
    ICommandPoolFactory commandPoolFactory,
    IRenderDataManager renderDataManager)
{
    private ILifetimeScope? _inputModuleLifetimeScope;
    private ILifetimeScope? _renderModuleLifetimeScope;
    private ModuleDataAccessor? _moduleDataAccessor;

    private IReadOnlyList<InputModuleReference>? _sortedInputModules;
    private readonly HashSet<Identification> _updatedInputData = new();
    private readonly HashSet<Identification> _updatedIntermediateData = new();

    private IReadOnlyList<RenderModule>? _sortedRenderModules;

    private ManagedCommandPool? _inputModuleCommandPool;
    private ManagedCommandBuffer? _inputModuleCommandBuffer;

    private volatile bool _isRunning;
    private Thread? _thread;

    public int MaxFps
    {
        set
        {
            _maxFps = value;
            _frameTime = 1f / _maxFps;
        }
    }

    private int _maxFps = 60;
    private float _frameTime;

    public int CurrentFps { get; private set; }

    //If this is set to another value than 1, update the logic two calculate the fps, by dividing the frame counter by the update interval
    private const int FpsUpdateInterval = 1;
    private float _lastFrameTimeUpdate;
    private int _frameCounter;

    private Stopwatch _stopwatch = new();


    public void Start()
    {
        Setup();
        _isRunning = true;

        _thread = new Thread(Work);
        _thread.Start();
    }

    public void Stop()
    {
        _isRunning = false;
        VulkanUtils.Assert(vulkanEngine.Vk.DeviceWaitIdle(vulkanEngine.Device));

        _thread?.Join();
        _thread = null;
        
        _inputFence.Dispose();

        foreach (var inputModule in _sortedInputModules ?? Enumerable.Empty<InputModuleReference>())
        {
            inputModule.Module.Dispose();
        }

        _sortedInputModules = null;
        _inputModuleLifetimeScope?.Dispose();
        _inputModuleLifetimeScope = null;

        foreach (var renderModule in _sortedRenderModules ?? Enumerable.Empty<RenderModule>())
        {
            renderModule.Dispose();
        }

        _sortedRenderModules = null;
        _renderModuleLifetimeScope?.Dispose();
        _renderModuleLifetimeScope = null;

        _updatedInputData.Clear();
        _updatedIntermediateData.Clear();

        _moduleDataAccessor = null;

        DestroyInputCommandBuffer();
    }

    //fence gets initialized in the setup method
    private ManagedFence _inputFence = null!;

    private void Work()
    {
        if (_moduleDataAccessor is null)
            throw new MintyCoreException("Render graph not setup");

        _stopwatch.Start();

        while (_isRunning)
        {
            NextFrame();

            if (!_isRunning)
                return;

            //By running the input and render process not completely async, we can avoid the need to sync the intermediate data
            var inputTask = BeginProcessingInputModules();
            var renderTask = BeginProcessingRenderModules();

            Task.WaitAll(inputTask, renderTask);

            SubmitInputWork();
            EndFrame();

            fenceAwaiter.AwaitAsync(_inputFence).Wait();
            _inputFence.Reset();

            _moduleDataAccessor.UpdateIntermediateData();
        }
    }

    private void NextFrame()
    {
        while (!vulkanEngine.PrepareDraw() && _isRunning)
        {
            if (!_isRunning)
                return;

            //the prepare draw only returns false if the window is minimized
            //therefore waiting a bit is fine
            Thread.Sleep(10);
        }

        var currentElapsed = _stopwatch.Elapsed.TotalSeconds;

        while (currentElapsed < _frameTime)
        {
            //TODO check if this is the best way to wait
            Thread.SpinWait(1);

            currentElapsed = _stopwatch.Elapsed.TotalSeconds;
        }

        _stopwatch.Restart();

        _lastFrameTimeUpdate += (float)currentElapsed;
        _frameCounter++;

        if (!(_lastFrameTimeUpdate >= FpsUpdateInterval)) return;

        CurrentFps = _frameCounter;
        _frameCounter = 0;
        _lastFrameTimeUpdate = 0;
    }


    private void Setup()
    {
        AllocateInputCommandBuffer();

        _inputFence = fenceFactory.CreateFence();

        _moduleDataAccessor = new ModuleDataAccessor(inputDataManager, intermediateDataManager, renderDataManager);

        var inputModules = inputModuleManager.CreateInputModuleInstances(out _inputModuleLifetimeScope);

        foreach (var inputModule in inputModules.Values)
        {
            inputModule.ModuleDataAccessor = _moduleDataAccessor;
            inputModule.Setup();
        }

        var renderModules = renderModuleManager.CreateRenderModuleInstances(out _renderModuleLifetimeScope);
        foreach (var renderModule in renderModules.Values)
        {
            renderModule.ModuleDataAccessor = _moduleDataAccessor;
            renderModule.Setup();
        }

        _moduleDataAccessor.ValidateIntermediateDataProvided();

        var sortedInputModules = _moduleDataAccessor.SortInputModules(inputModules.Keys);

        _sortedInputModules = sortedInputModules.Select(id => new InputModuleReference
        {
            Module = inputModules[id],
            InputDataIds = _moduleDataAccessor.GetInputModuleConsumedInputDataIds(id).ToList(),
            ConsumedIntermediateIds = _moduleDataAccessor.GetInputModuleConsumedIntermediateDataIds(id).ToList(),
            ProvidedIntermediateIds = _moduleDataAccessor.GetInputModuleProvidedIntermediateDataIds(id).ToList()
        }).ToList();

        var renderModuleGraph = new AdjacencyGraph<Identification, Edge<Identification>>();
        renderModuleGraph.AddVertexRange(renderModules.Keys);

        foreach (var (id, module) in renderModules)
        {
            foreach (var after in module.ExecuteAfter)
            {
                if (!renderModuleGraph.ContainsVertex(after))
                    Log.Warning(
                        "Render module {Module} has an execute after dependency on {After}, which is not an active render module",
                        id, after);
                renderModuleGraph.AddEdge(new Edge<Identification>(after, id));
            }

            foreach (var before in module.ExecuteBefore)
            {
                if (!renderModuleGraph.ContainsVertex(before))
                    Log.Warning(
                        "Render module {Module} has an execute before dependency on {Before}, which is not an active render module",
                        id, before);

                renderModuleGraph.AddEdge(new Edge<Identification>(id, before));
            }
        }

        if (!renderModuleGraph.IsDirectedAcyclicGraph())
            throw new MintyCoreException("Render modules have a circular dependency. This is not allowed.");

        _sortedRenderModules = renderModuleGraph.TopologicalSort().Select(id => renderModules[id]).ToList();
    }

    private Task BeginProcessingInputModules()
    {
        _updatedInputData.Clear();
        _updatedIntermediateData.Clear();

        PrepareInputCommandBuffer();

        _updatedInputData.UnionWith(inputDataManager.GetUpdatedInputDataIds(reset: true));

        return Task.Run(ProcessInputModules);
    }

    private unsafe void SubmitInputWork()
    {
        if (_inputModuleCommandBuffer is null)
            throw new MintyCoreException("Input command buffer not allocated");

        _inputModuleCommandBuffer.EndCommandBuffer();

        var internalCommandBuffer = _inputModuleCommandBuffer.InternalCommandBuffer;


        var submitInfo = new SubmitInfo
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &internalCommandBuffer
        };

        lock (vulkanEngine.GraphicQueue.queueLock)
            VulkanUtils.Assert(vulkanEngine.Vk.QueueSubmit(vulkanEngine.GraphicQueue.queue, 1, &submitInfo,
                _inputFence.Fence));
    }

    private void ProcessInputModules()
    {
        if (_sortedInputModules is null || _moduleDataAccessor is null || _inputModuleCommandBuffer is null)
        {
            throw new MintyCoreException("Render graph not setup");
        }

        foreach (var inputModuleReference in _sortedInputModules)
        {
            var consumedInputData = inputModuleReference.InputDataIds;
            var consumedIntermediateData = inputModuleReference.ConsumedIntermediateIds;
            var providedIntermediateData = inputModuleReference.ProvidedIntermediateIds;

            //check if at least one input or intermediate data is updated
            if (!consumedInputData.Any(_updatedInputData.Contains) &&
                !consumedIntermediateData.Any(_updatedIntermediateData.Contains))
                continue;

            inputModuleReference.Module.Update(_inputModuleCommandBuffer);

            _updatedIntermediateData.UnionWith(providedIntermediateData);
        }
    }

    private Task BeginProcessingRenderModules()
    {
        var commandBuffer = vulkanEngine.GetRenderCommandBuffer();

        _currentSwapchainImage = vulkanEngine.SwapchainImages[vulkanEngine.SwapchainImageIndex];

        var subResourceRange = new ImageSubresourceRange
        {
            AspectMask = ImageAspectFlags.ColorBit,
            LevelCount = Vk.RemainingMipLevels,
            LayerCount = Vk.RemainingArrayLayers
        };
        Span<ImageSubresourceRange> subResourceRangeSpan = [subResourceRange];

        Span<ImageMemoryBarrier> barrier =
        [
            new ImageMemoryBarrier
            {
                SType = StructureType.ImageMemoryBarrier,
                Image = _currentSwapchainImage,
                OldLayout = ImageLayout.Undefined,
                NewLayout = ImageLayout.TransferDstOptimal,
                SrcAccessMask = 0,
                DstAccessMask = AccessFlags.TransferWriteBit,
                SubresourceRange = subResourceRange
            }
        ];

        commandBuffer.PipelineBarrier(PipelineStageFlags.TopOfPipeBit, PipelineStageFlags.TransferBit, 0, barrier);

        var clearColorValue = new ClearColorValue(0, 0, 0, 0);
        vulkanEngine.Vk.CmdClearColorImage(commandBuffer.InternalCommandBuffer, _currentSwapchainImage,
            ImageLayout.TransferDstOptimal, clearColorValue, subResourceRangeSpan);

        barrier[0].OldLayout = ImageLayout.TransferDstOptimal;
        barrier[0].NewLayout = ImageLayout.ColorAttachmentOptimal;
        barrier[0].SrcAccessMask = AccessFlags.TransferWriteBit;
        barrier[0].DstAccessMask = AccessFlags.ColorAttachmentWriteBit;

        commandBuffer.PipelineBarrier(PipelineStageFlags.TransferBit, PipelineStageFlags.ColorAttachmentOutputBit, 0,
            barrier);

        return Task.Run(ProcessRenderModules);
    }

    private void EndFrame()
    {
        var commandBuffer = vulkanEngine.GetRenderCommandBuffer();

        var subResourceRange = new ImageSubresourceRange
        {
            AspectMask = ImageAspectFlags.ColorBit,
            LevelCount = Vk.RemainingMipLevels,
            LayerCount = Vk.RemainingArrayLayers
        };

        Span<ImageMemoryBarrier> barrier =
        [
            new ImageMemoryBarrier
            {
                SType = StructureType.ImageMemoryBarrier,
                Image = _currentSwapchainImage,
                OldLayout = ImageLayout.ColorAttachmentOptimal,
                NewLayout = ImageLayout.PresentSrcKhr,
                SrcAccessMask = AccessFlags.ColorAttachmentWriteBit,
                DstAccessMask = AccessFlags.MemoryReadBit,
                SubresourceRange = subResourceRange
            }
        ];

        commandBuffer.PipelineBarrier(PipelineStageFlags.ColorAttachmentOutputBit, PipelineStageFlags.BottomOfPipeBit,
            0, barrier);

        vulkanEngine.EndDraw();

        _currentSwapchainImage = default;
        _usedRenderTextures.Clear();
        _lastUsageKind.Clear();
    }

    private unsafe void ProcessRenderModules()
    {
        var commandBuffer = vulkanEngine.GetRenderCommandBuffer();

        if (_sortedRenderModules is null || _moduleDataAccessor is null)
        {
            throw new MintyCoreException("Render graph not setup");
        }

        foreach (var renderModule in _sortedRenderModules)
        {
            var id = renderModule.Identification;

            var sampledTextures = _moduleDataAccessor.GetRenderModuleSampledTexturesAccessed(id);
            foreach (var texture in sampledTextures)
            {
                SetImageBarrier(commandBuffer, texture, UsageKind.Sampled);
            }

            var storageTextures = _moduleDataAccessor.GetRenderModuleStorageTexturesAccessed(id);
            foreach (var texture in storageTextures)
            {
                SetImageBarrier(commandBuffer, texture, UsageKind.Storage);
            }

            var colorAttachment = _moduleDataAccessor.GetRenderModuleColorAttachment(id);

            // ReSharper disable once TooWideLocalVariableScope - dont want to risk that the stack is unwound and the pointer is invalid
            RenderingAttachmentInfo colorAttachmentInfo;
            RenderingAttachmentInfo* colorAttachmentInfoPtr = null;
            if (colorAttachment is not null)
            {
                colorAttachment.Value.Switch(
                    attachmentId => SetImageBarrier(commandBuffer, attachmentId, UsageKind.RenderTarget),
                    _ => SetSwapchainImageBarrier(commandBuffer));

                var imageView = colorAttachment.Value.Match(
                    renderDataManager.GetRenderImageView,
                    _ => vulkanEngine.SwapchainImageViews[vulkanEngine.SwapchainImageIndex]
                );

                colorAttachmentInfo = new()
                {
                    SType = StructureType.RenderingAttachmentInfo,
                    ImageLayout = ImageLayout.AttachmentOptimal,
                    ImageView = imageView,
                    LoadOp = AttachmentLoadOp.Load,
                    StoreOp = AttachmentStoreOp.Store,
                };
                colorAttachmentInfoPtr = &colorAttachmentInfo;
            }

            var depthStencilAttachment = _moduleDataAccessor.GetRenderModuleDepthStencilAttachment(id);
            // ReSharper disable once TooWideLocalVariableScope - dont want to risk that the stack is unwound and the pointer is invalid
            RenderingAttachmentInfo depthStencilAttachmentInfo;
            RenderingAttachmentInfo* depthStencilAttachmentInfoPtr = null;
            if (depthStencilAttachment is not null)
            {
                SetImageBarrier(commandBuffer, depthStencilAttachment.Value, UsageKind.DepthStencil);

                depthStencilAttachmentInfo = new()
                {
                    SType = StructureType.RenderingAttachmentInfo,
                    ImageLayout = ImageLayout.DepthStencilAttachmentOptimal,
                    ImageView = renderDataManager.GetRenderImageView(depthStencilAttachment.Value),
                    LoadOp = AttachmentLoadOp.Load,
                    StoreOp = AttachmentStoreOp.Store,
                };
                depthStencilAttachmentInfoPtr = &depthStencilAttachmentInfo;
            }

            var renderingInfo = new RenderingInfo
            {
                SType = StructureType.RenderingInfo,
                LayerCount = 1,
                ColorAttachmentCount = colorAttachmentInfoPtr is not null ? 1u : 0u,
                PColorAttachments = colorAttachmentInfoPtr,
                RenderArea = new Rect2D(new Offset2D(0, 0), vulkanEngine.SwapchainExtent),
                PDepthAttachment = depthStencilAttachmentInfoPtr
            };

            vulkanEngine.Vk.CmdBeginRendering(commandBuffer.InternalCommandBuffer, renderingInfo);

            renderModule.Render(commandBuffer);

            vulkanEngine.Vk.CmdEndRendering(commandBuffer.InternalCommandBuffer);
        }
    }

    Image _currentSwapchainImage;
    private readonly Dictionary<Identification, Texture> _usedRenderTextures = new();
    private readonly Dictionary<Identification, UsageKind> _lastUsageKind = new();

    enum UsageKind
    {
        RenderTarget,
        Sampled,
        Storage,
        DepthStencil
    }

    private void SetImageBarrier(ManagedCommandBuffer commandBuffer, Identification id, UsageKind usageKind)
    {
        if (!_usedRenderTextures.TryGetValue(id, out var texture))
        {
            InitializeTextureWithBarrier(commandBuffer, id, usageKind);
            return;
        }

        var lastKind = _lastUsageKind[id];

        var barrier = new ImageMemoryBarrier
        {
            SType = StructureType.ImageMemoryBarrier,
            Image = texture.Image,
            OldLayout = GetImageLayout(lastKind),
            NewLayout = GetImageLayout(usageKind),
            SrcAccessMask = GetAccessFlag(lastKind),
            DstAccessMask = GetAccessFlag(usageKind),
            SubresourceRange =
            {
                AspectMask = GetAspectMask(usageKind, texture),
                LayerCount = Vk.RemainingArrayLayers,
                LevelCount = Vk.RemainingMipLevels
            }
        };

        commandBuffer.PipelineBarrier(GetPipelineStageFlag(lastKind), GetPipelineStageFlag(usageKind), 0, barrier);
    }


    private void InitializeTextureWithBarrier(ManagedCommandBuffer commandBuffer, Identification id,
        UsageKind usageKind)
    {
        var texture = renderDataManager.GetRenderTexture(id);
        _usedRenderTextures.Add(id, texture);
        _lastUsageKind.Add(id, usageKind);
        
        var aspectMask = GetAspectMask(usageKind, texture);

        var barrier = new ImageMemoryBarrier
        {
            SType = StructureType.ImageMemoryBarrier,
            Image = texture.Image,
            OldLayout = ImageLayout.Undefined,
            NewLayout = ImageLayout.TransferDstOptimal,
            SrcAccessMask = 0,
            DstAccessMask = AccessFlags.TransferWriteBit,
            SubresourceRange =
            {
                AspectMask = aspectMask,
                LayerCount = Vk.RemainingArrayLayers,
                LevelCount = Vk.RemainingMipLevels
            }
        };

        commandBuffer.PipelineBarrier(PipelineStageFlags.TopOfPipeBit, PipelineStageFlags.TransferBit, 0, barrier);

        if (usageKind == UsageKind.DepthStencil)
            commandBuffer.ClearDepthStencilImage(texture, new ClearDepthStencilValue(1, 0),
                new ImageSubresourceRange
                {
                    AspectMask = ImageAspectFlags.DepthBit,
                    LayerCount = Vk.RemainingArrayLayers,
                    LevelCount = Vk.RemainingMipLevels
                }, ImageLayout.TransferDstOptimal);
        else
            commandBuffer.ClearColorImage(texture,
                renderDataManager.GetClearColorValue(id) ?? new ClearColorValue(0, 0, 0, 0),
                new ImageSubresourceRange
                {
                    AspectMask = ImageAspectFlags.ColorBit, LayerCount = Vk.RemainingArrayLayers,
                    LevelCount = Vk.RemainingMipLevels
                }, ImageLayout.TransferDstOptimal);

        barrier.OldLayout = ImageLayout.TransferDstOptimal;
        barrier.NewLayout = GetImageLayout(usageKind);
        barrier.SrcAccessMask = AccessFlags.TransferWriteBit;
        barrier.DstAccessMask = GetAccessFlag(usageKind);

        commandBuffer.PipelineBarrier(PipelineStageFlags.TransferBit, GetPipelineStageFlag(usageKind), 0, barrier);
    }

    private static AccessFlags GetAccessFlag(UsageKind usageKind)
    {
        return usageKind switch
        {
            UsageKind.RenderTarget => AccessFlags.ColorAttachmentWriteBit,
            UsageKind.Sampled => AccessFlags.ShaderReadBit,
            UsageKind.Storage => AccessFlags.ShaderReadBit | AccessFlags.ShaderWriteBit,
            UsageKind.DepthStencil => AccessFlags.DepthStencilAttachmentReadBit |
                                      AccessFlags.DepthStencilAttachmentWriteBit,
            _ => throw new MintyCoreException("Unknown usage kind")
        };
    }

    private static PipelineStageFlags GetPipelineStageFlag(UsageKind usageKind)
    {
        return usageKind switch
        {
            UsageKind.RenderTarget => PipelineStageFlags.ColorAttachmentOutputBit,
            UsageKind.Sampled => PipelineStageFlags.FragmentShaderBit,
            UsageKind.Storage => PipelineStageFlags.FragmentShaderBit,
            UsageKind.DepthStencil => PipelineStageFlags.EarlyFragmentTestsBit |
                                      PipelineStageFlags.LateFragmentTestsBit,
            _ => throw new MintyCoreException("Unknown usage kind")
        };
    }

    private static ImageLayout GetImageLayout(UsageKind usageKind)
    {
        return usageKind switch
        {
            UsageKind.RenderTarget => ImageLayout.ColorAttachmentOptimal,
            UsageKind.Sampled => ImageLayout.ShaderReadOnlyOptimal,
            UsageKind.Storage => ImageLayout.General,
            UsageKind.DepthStencil => ImageLayout.DepthStencilAttachmentOptimal,
            _ => throw new MintyCoreException("Unknown usage kind")
        };
    }

    private static ImageAspectFlags GetAspectMask(UsageKind usageKind, Texture texture)
    {
        var aspectMask = usageKind == UsageKind.DepthStencil
            ? ImageAspectFlags.DepthBit
            : ImageAspectFlags.ColorBit;

        if (FormatHelpers.IsStencilFormat(texture.Format))
            aspectMask |= ImageAspectFlags.StencilBit;
        return aspectMask;
    }

    private void SetSwapchainImageBarrier(ManagedCommandBuffer commandBuffer)
    {
        var subResourceRange = new ImageSubresourceRange
        {
            AspectMask = ImageAspectFlags.ColorBit,
            LevelCount = Vk.RemainingMipLevels,
            LayerCount = Vk.RemainingArrayLayers
        };

        Span<ImageMemoryBarrier> barrier =
        [
            new ImageMemoryBarrier
            {
                SType = StructureType.ImageMemoryBarrier,
                Image = _currentSwapchainImage,
                OldLayout = ImageLayout.ColorAttachmentOptimal,
                NewLayout = ImageLayout.ColorAttachmentOptimal,
                SrcAccessMask = AccessFlags.ColorAttachmentWriteBit,
                DstAccessMask = AccessFlags.ColorAttachmentWriteBit,
                SubresourceRange = subResourceRange
            }
        ];

        commandBuffer.PipelineBarrier(PipelineStageFlags.ColorAttachmentOutputBit,
            PipelineStageFlags.ColorAttachmentOutputBit, 0, barrier);
    }

    private void AllocateInputCommandBuffer()
    {
        _inputModuleCommandPool =
            commandPoolFactory.CreateCommandPool(new CommandPoolDescription(vulkanEngine.GraphicQueue.familyIndex,
                true));

        _inputModuleCommandBuffer = _inputModuleCommandPool.AllocateCommandBuffer(CommandBufferLevel.Primary);
    }

    private void PrepareInputCommandBuffer()
    {
        _inputModuleCommandBuffer!.BeginCommandBuffer(CommandBufferUsageFlags.OneTimeSubmitBit);
    }

    private void DestroyInputCommandBuffer()
    {
        _inputModuleCommandBuffer?.Dispose();

        _inputModuleCommandBuffer = default;
        
        _inputModuleCommandPool?.Dispose();
        _inputModuleCommandPool = default;
    }

    private record struct InputModuleReference(
        InputModule Module,
        IReadOnlyList<Identification> InputDataIds,
        IReadOnlyList<Identification> ConsumedIntermediateIds,
        IReadOnlyList<Identification> ProvidedIntermediateIds);
}