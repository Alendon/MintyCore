using System;
using JetBrains.Annotations;
using MintyCore.Graphics.Managers;
using MintyCore.Graphics.Utils;
using MintyCore.Utils;
using Serilog;

namespace MintyCore.Graphics.Render.Managers.Implementations;

[Singleton<IRenderManager>(SingletonContextFlags.NoHeadless)]
public class RenderManager(
    IInputDataManager inputDataManager,
    IInputModuleManager inputModuleManager,
    IIntermediateDataManager intermediateDataManager,
    IAsyncFenceAwaiter fenceAwaiter,
    IVulkanEngine vulkanEngine,
    IFenceFactory fenceFactory,
    IRenderModuleManager renderModuleManager,
    ICommandPoolFactory commandPoolFactory,
    IRenderPassManager renderPassManager,
    IRenderDataManager renderDataManager)
    : IRenderManager
{
    private RenderGraph? _renderGraph;
    private int _maxFrameRate;

    public void StartRendering()
    {
        if (_renderGraph is not null)
            throw new InvalidOperationException("Rendering is already running");

        _renderGraph = new RenderGraph(inputDataManager, inputModuleManager, intermediateDataManager, fenceAwaiter,
            vulkanEngine, fenceFactory, renderModuleManager, commandPoolFactory, renderPassManager, renderDataManager)
        {
            MaxFps = MaxFrameRate
        };

        _renderGraph.Start();
    }

    public void StopRendering()
    {
        if (_renderGraph is null)
            Log.Error("Rendering is not running");

        _renderGraph?.Stop();
        _renderGraph = null;
    }

    public int MaxFrameRate
    {
        get => _maxFrameRate;
        set
        {
            if (_renderGraph is not null)
                _renderGraph.MaxFps = value;

            _maxFrameRate = value;
        }
    }

    public int FrameRate => _renderGraph?.CurrentFps ?? 0;
}