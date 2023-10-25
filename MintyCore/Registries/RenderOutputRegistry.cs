using System.Collections.Generic;
using System.Linq;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Modding.Attributes;
using MintyCore.Modding.Implementations;
using MintyCore.Render;
using MintyCore.Utils;

namespace MintyCore.Registries;

[Registry("render_output", applicableGameType: GameType.Client)]
public class RenderOutputRegistry : IRegistry
{
    public required IRenderOutputManager RenderOutputManager { private get; init; }

    /// <inheritdoc />
    public ushort RegistryId => RegistryIDs.RenderOutput;

    /// <inheritdoc />
    public IEnumerable<ushort> RequiredRegistries => Enumerable.Empty<ushort>();


    [RegisterMethod(ObjectRegistryPhase.Main)]
    public void RegisterRenderOutput<TRenderOutput>(Identification id) where TRenderOutput : class
    {
        RenderOutputManager.AddRenderOutput<TRenderOutput>(id);
    }

    /// <inheritdoc />
    public void UnRegister(Identification objectId)
    {
        RenderOutputManager.RemoveRenderOutput(objectId);
    }

    /// <inheritdoc />
    public void Clear()
    {
    }
}