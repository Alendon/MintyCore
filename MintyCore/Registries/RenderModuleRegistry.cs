using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using MintyCore.Graphics.Render;
using MintyCore.Graphics.Render.Managers;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Modding.Attributes;
using MintyCore.Modding.Implementations;
using MintyCore.Utils;

namespace MintyCore.Registries;

/// <summary>
///  Registry to manage render modules
/// </summary>
[Registry("render_module")]
public class RenderModuleRegistry : IRegistry
{
    /// <inheritdoc />
    public ushort RegistryId => RegistryIDs.RenderModule;
    
    /// <inheritdoc />
    public IEnumerable<ushort> RequiredRegistries => Enumerable.Empty<ushort>();
    
    /// <summary/>
    public required IRenderModuleManager RenderModuleManager { private get; [UsedImplicitly] set; }
    
    /// <summary>
    ///  Register a render module
    /// </summary>
    /// <param name="id"> Id of the render module</param>
    /// <typeparam name="TRenderModule"> Type of the render module</typeparam>
    [RegisterMethod(ObjectRegistryPhase.Main)]
    public void RegisterRenderModule<TRenderModule>(Identification id) where TRenderModule : RenderModule
    {
        RenderModuleManager.RegisterRenderModule<TRenderModule>(id);
    }
    
    /// <inheritdoc />
    public void UnRegister(Identification objectId)
    {
        RenderModuleManager.UnRegisterRenderModule(objectId);
    }
 
    /// <inheritdoc />
    public void Clear()
    {
        RenderModuleManager.Clear();
    }
}