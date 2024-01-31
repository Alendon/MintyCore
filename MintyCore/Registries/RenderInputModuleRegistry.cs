using System.Collections.Generic;
using System.Linq;
using MintyCore.Graphics.Render;
using MintyCore.Graphics.Render.Managers;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Modding.Attributes;
using MintyCore.Modding.Implementations;
using MintyCore.Utils;

namespace MintyCore.Registries;

[Registry("render_input_module")]
public class RenderInputModuleRegistry : IRegistry
{
    public ushort RegistryId => RegistryIDs.RenderInputModule;
    public IEnumerable<ushort> RequiredRegistries => Enumerable.Empty<ushort>();

    public required IInputModuleManager InputModuleManager { private get; set; }

    [RegisterMethod(ObjectRegistryPhase.Main)]
    public void RegisterInputDataModule<TInputDataModule>(Identification id) where TInputDataModule : InputModule
    {
        InputModuleManager.RegisterInputModule<TInputDataModule>(id);
    }

    public void UnRegister(Identification objectId)
    {
        throw new System.NotImplementedException();
    }

    public void Clear()
    {
        throw new System.NotImplementedException();
    }
}