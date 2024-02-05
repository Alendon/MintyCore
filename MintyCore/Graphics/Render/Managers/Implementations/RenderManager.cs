using System;
using JetBrains.Annotations;
using MintyCore.Graphics.Managers;
using MintyCore.Graphics.Utils;
using MintyCore.Utils;
using Serilog;

namespace MintyCore.Graphics.Render.Managers.Implementations;

[Singleton<IRenderManager>(SingletonContextFlags.NoHeadless)]
public class RenderManager : IRenderManager
{
    private RenderGraph? _renderGraph;
    private int _maxFrameRate;

    public required IInputDataManager inputDataManager { private get; [UsedImplicitly] init; }
    public required IInputModuleManager inputModuleManager { private get; [UsedImplicitly] init; }
    public required IIntermediateDataManager intermediateDataManager { private get; [UsedImplicitly] init; }
    public required IAsyncFenceAwaiter fenceAwaiter { private get; [UsedImplicitly] init; }
    public required IVulkanEngine vulkanEngine { private get; [UsedImplicitly] init; }
    public required IFenceFactory fenceFactory { private get; [UsedImplicitly] init; }
    public required IRenderModuleManager renderModuleManager { private get; [UsedImplicitly] init; }
    public required ICommandPoolFactory commandPoolFactory { private get; [UsedImplicitly] init; }
    public required IRenderPassManager renderPassManager { private get; [UsedImplicitly] init; }

    public void StartRendering()
    {
        if (_renderGraph is not null)
            throw new InvalidOperationException("Rendering is already running");

        _renderGraph = new RenderGraph(inputDataManager, inputModuleManager, intermediateDataManager, fenceAwaiter,
            vulkanEngine, fenceFactory, renderModuleManager, commandPoolFactory, renderPassManager)
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