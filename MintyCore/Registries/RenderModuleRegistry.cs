using System.Collections.Generic;
using System.Linq;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Modding.Attributes;
using MintyCore.Modding.Implementations;
using MintyCore.Render;
using MintyCore.Utils;

namespace MintyCore.Registries;

[Registry("render_module", applicableGameType: GameType.Client)]
public class RenderModuleRegistry : IRegistry
{
    /// <inheritdoc />
    public ushort RegistryId => RegistryIDs.RenderModule;

    /// <inheritdoc />
    public IEnumerable<ushort> RequiredRegistries => Enumerable.Empty<ushort>();

    public required IRenderManager RenderManager { private get; init; }

    [RegisterMethod(ObjectRegistryPhase.Main)]
    public void RegisterRenderModule<TRenderModule>(Identification identification) where TRenderModule : IRenderModule
    {
        RenderManager.AddRenderModule<TRenderModule>(identification);
    }

    /// <inheritdoc />
    public void UnRegister(Identification objectId)
    {
        RenderManager.RemoveRenderModule(objectId);
    }

    /// <inheritdoc />
    public void PostRegister(ObjectRegistryPhase currentPhase)
    {
        if(currentPhase == ObjectRegistryPhase.Main)
            RenderManager.ConstructRenderModules();
    }

    /// <inheritdoc />
    public void Clear()
    {
    }
}