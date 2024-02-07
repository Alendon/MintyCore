using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using MintyCore.Graphics.Render;
using MintyCore.Graphics.Render.Managers;
using MintyCore.Identifications;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Modding.Attributes;
using MintyCore.Modding.Implementations;
using MintyCore.Utils;

namespace MintyCore.Registries;

[Registry("render_module")]
public class RenderModuleRegistry : IRegistry
{
    public ushort RegistryId => RegistryIDs.RenderModule;
    public IEnumerable<ushort> RequiredRegistries => Enumerable.Empty<ushort>();
    
    public required IRenderModuleManager RenderModuleManager { private get; [UsedImplicitly] set; }
    
    [RegisterMethod(ObjectRegistryPhase.Main)]
    public void RegisterRenderModule<TRenderModule>(Identification id) where TRenderModule : RenderModule
    {
        RenderModuleManager.RegisterRenderModule<TRenderModule>(id);
    }
    
    
    public void UnRegister(Identification objectId)
    {
        RenderModuleManager.UnRegisterRenderModule(objectId);
    }

    public void Clear()
    {
        RenderModuleManager.Clear();
    }
}