using System.Collections.Generic;
using Autofac;
using MintyCore.Utils;

namespace MintyCore.Graphics.Render.Managers;

public interface IRenderModuleManager
{
    public void RegisterRenderModule<TRenderModule>(Identification identification) where TRenderModule : RenderModule;
    IReadOnlySet<Identification> RegisteredRenderModuleIds { get; }
    Dictionary<Identification, RenderModule> CreateRenderModuleInstances(out ILifetimeScope lifetimeScope);
    void SetModuleActive(Identification moduleId, bool isActive);
    bool IsModuleActive(Identification moduleId);
    void UnRegisterRenderModule(Identification objectId);
    void Clear();
}