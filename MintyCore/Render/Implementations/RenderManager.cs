using System;
using System.Collections.Generic;
using System.Threading;
using Autofac;
using MintyCore.Utils;
using Serilog;

namespace MintyCore.Render.Implementations;

[Singleton<IRenderManager>(SingletonContextFlags.NoHeadless)]
internal sealed class RenderManager : IRenderManager
{
    private readonly Dictionary<Identification, Action<ContainerBuilder>> _renderModuleBuilders = new();


    private HashSet<Identification> _activeRenderModules = new();

    private ILifetimeScope? _renderModuleScope;

    public required IRenderInputManager RenderInputManager { private get; init; }
    public required IVulkanEngine VulkanEngine { private get; init; }
    public required ILifetimeScope LifetimeScope { private get; init; }

    /// <inheritdoc />
    public void AddRenderModule<TRenderModule>(Identification renderModuleId) where TRenderModule : IRenderModule
    {
        _renderModuleBuilders.Add(
            renderModuleId,
            builder => builder.RegisterType<TRenderModule>().Keyed<IRenderModule>(renderModuleId)
                .SingleInstance());
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


    /// <inheritdoc />
    public void StartRendering()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public void StopRendering()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public bool IsRendering { get; private set; }

    /// <inheritdoc />
    public void Recreate()
    {
        var wasRendering = IsRendering;

        if (wasRendering)
        {
            StopRendering();
        }

        VulkanEngine.RecreateSwapchain();

        if (wasRendering)
        {
            StartRendering();
        }
    }

    private volatile bool _workerRunning;

    private Thread? _workerThread = null;

    private void Worker()
    {
        while (_workerRunning)
        {
            //start input processing
            var inputTask = RenderInputManager.ProcessAll();
            //explicitly start input processing. Although this shouldn't be necessary
            inputTask.Start();

            //vulkan start frame
            if (!VulkanEngine.PrepareDraw())
            {
                //TODO make sure that this never happens
                throw new MintyCoreException("Oh no, vulkan failed to prepare drawing");
            }

            //wait for input processing
            inputTask.Wait();

            //execute render modules


            //vulkan submit frame
            VulkanEngine.EndDraw();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _workerRunning = false;
        _workerThread?.Join();
        _renderModuleScope?.Dispose();
    }
}