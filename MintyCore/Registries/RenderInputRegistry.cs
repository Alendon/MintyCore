using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Modding.Attributes;
using MintyCore.Modding.Implementations;
using MintyCore.Render;
using MintyCore.Utils;

namespace MintyCore.Registries;

/// <inheritdoc />
[Registry("render_input", applicableGameType: GameType.Client)]
public class RenderInputRegistry : IRegistry
{
    /// <inheritdoc />
    public ushort RegistryId => RegistryIDs.RenderInput;

    /// <inheritdoc />
    public IEnumerable<ushort> RequiredRegistries { get; } = Enumerable.Empty<ushort>();
    
    /// <summary/>
    public required IRenderInputManager RenderInputManager { private get; [UsedImplicitly] init; }

    [RegisterMethod(ObjectRegistryPhase.Main)]
    public void RegisterRenderInput<TRenderInput>(Identification objectId) where TRenderInput : IRenderInput
    {
        RenderInputManager.AddRenderInput<TRenderInput>(objectId);
    }

    /// <inheritdoc />
    public void PostRegister(ObjectRegistryPhase currentPhase)
    {
        if(currentPhase == ObjectRegistryPhase.Main)
            RenderInputManager.ConstructRenderInputs();
    }

    public void UnRegister(Identification objectId)
    {
        RenderInputManager.RemoveRenderInput(objectId);
    }

    public void Clear()
    {
        
    }
}