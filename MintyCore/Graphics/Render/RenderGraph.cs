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
using MintyCore.Identifications;
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
    IRenderPassManager renderPassManager)
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
        get => _maxFps;
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

        _moduleDataAccessor?.Clear();
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

        _moduleDataAccessor = new ModuleDataAccessor(inputDataManager, intermediateDataManager);

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

    private unsafe Task BeginProcessingRenderModules()
    {
        var commandBuffer = vulkanEngine.GetRenderCommandBuffer().InternalCommandBuffer;
        var renderPass = renderPassManager.GetRenderPass(RenderPassIDs.SwapchainRenderPass);
        var framebuffer = vulkanEngine.SwapchainFramebuffers[vulkanEngine.ImageIndex];

        var clearValue = new ClearValue { Color = new ClearColorValue(0, 0, 0, 1) };

        var renderingInfo = new RenderPassBeginInfo()
        {
            SType = StructureType.RenderPassBeginInfo,
            RenderArea = new Rect2D(new Offset2D(0, 0), vulkanEngine.SwapchainExtent),
            RenderPass = renderPass,
            Framebuffer = framebuffer,
            ClearValueCount = 1,
            PClearValues = &clearValue
        };

        vulkanEngine.Vk.CmdBeginRenderPass(commandBuffer, renderingInfo, SubpassContents.Inline);

        return Task.Run(ProcessRenderModules);
    }

    private void EndFrame()
    {
        var commandBuffer = vulkanEngine.GetRenderCommandBuffer().InternalCommandBuffer;
        
        vulkanEngine.Vk.CmdEndRenderPass(commandBuffer);

        vulkanEngine.EndDraw();
    }

    private void ProcessRenderModules()
    {
        var commandBuffer = vulkanEngine.GetRenderCommandBuffer();

        if (_sortedRenderModules is null || _moduleDataAccessor is null)
        {
            throw new MintyCoreException("Render graph not setup");
        }

        foreach (var renderModule in _sortedRenderModules)
        {
            renderModule.Render(commandBuffer);
        }
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
        _inputModuleCommandPool = default;
    }

    private record struct InputModuleReference(
        InputModule Module,
        IReadOnlyList<Identification> InputDataIds,
        IReadOnlyList<Identification> ConsumedIntermediateIds,
        IReadOnlyList<Identification> ProvidedIntermediateIds);
}