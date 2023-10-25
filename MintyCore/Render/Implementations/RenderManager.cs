using System;
using System.Collections.Generic;
using Autofac;
using JetBrains.Annotations;
using MintyCore.Utils;
using Serilog;

namespace MintyCore.Render.Implementations;

[Singleton<IRenderManager>(SingletonContextFlags.NoHeadless)]
internal sealed class RenderManager : IRenderManager
{
    private readonly Dictionary<Identification, Action<ContainerBuilder>> _renderModuleBuilders = new();


    private readonly HashSet<Identification> _activeRenderModules = new();

    private ILifetimeScope? _renderModuleScope;

    public required IRenderInputManager RenderInputManager { private get; [UsedImplicitly] init; }
    public required IRenderOutputManager OutputManager { private get; [UsedImplicitly] init; }
    public required IVulkanEngine VulkanEngine { private get; [UsedImplicitly] init; }
    public required ILifetimeScope LifetimeScope { private get; [UsedImplicitly] init; }

    /// <inheritdoc />
    public void AddRenderModule<TRenderModule>(Identification renderModuleId) where TRenderModule : IRenderModule
    {
        _renderModuleBuilders.Add(
            renderModuleId,
            builder => builder.RegisterType<TRenderModule>().Keyed<IRenderModule>(renderModuleId)
                .SingleInstance().PropertiesAutowired());
    }

    /// <inheritdoc />
    public void RemoveRenderModule(Identification renderModuleId)
    {
        _renderModuleScope?.Dispose();
        _renderModuleScope = null;

        _renderModuleBuilders.Remove(renderModuleId);
    }

    /// <inheritdoc />
    public IRenderModule GetRenderModule(Identification renderModuleId)
    {
        if (_renderModuleScope is null)
            throw new MintyCoreException("RenderModuleScope is null");

        return _renderModuleScope.ResolveKeyed<IRenderModule>(renderModuleId);
    }

    /// <inheritdoc />
    public void ConstructRenderModules()
    {
        if (_renderModuleScope is not null)
        {
            Log.Warning("RenderModuleScope is not null, disposing");

            _renderModuleScope.Dispose();
            _renderModuleScope = null;
        }

        _renderModuleScope = LifetimeScope.BeginLifetimeScope(builder =>
        {
            foreach (var renderModuleBuilder in _renderModuleBuilders)
            {
                renderModuleBuilder.Value(builder);
            }
        });
    }

    /// <inheritdoc />
    public void SetRenderModuleActive(Identification renderModuleId, bool active)
    {
        if (active)
        {
            _activeRenderModules.Add(renderModuleId);
        }
        else
        {
            _activeRenderModules.Remove(renderModuleId);
        }
    }

    /// <inheritdoc />
    public bool IsRenderModuleActive(Identification renderModuleId)
    {
        return _activeRenderModules.Contains(renderModuleId);
    }

    public IEnumerable<Identification> ActiveRenderModules => _activeRenderModules;

    //IRenderWorkerProperty with lazy loading
    private IRenderWorker? _renderWorker;

    private IRenderWorker RenderWorker
    {
        get { return _renderWorker ??= new RenderWorker(RenderInputManager, this, VulkanEngine, OutputManager); }
    }

    /// <inheritdoc />
    public void StartRendering()
    {
        if (IsRendering)
        {
            Log.Error("Rendering already started");
            return;
        }

        RenderWorker.Start();
    }

    /// <inheritdoc />
    public void StopRendering()
    {
        if (!IsRendering)
        {
            Log.Error("Rendering not started");
            return;
        }

        RenderWorker.Stop();
    }

    /// <inheritdoc />
    public bool IsRendering => RenderWorker.IsRunning();

    /// <inheritdoc />
    public void Recreate()
    {
        var wasRendering = IsRendering;

        if (wasRendering)
        {
            StopRendering();
        }

        VulkanEngine.RecreateSwapchain();
        RenderInputManager.RecreateGpuData();

        if (wasRendering)
        {
            StartRendering();
        }
    }


    /// <inheritdoc />
    public void Dispose()
    {
        _renderModuleScope?.Dispose();
    }
}