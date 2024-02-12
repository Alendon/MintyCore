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
///  Registry to manage render input module
/// </summary>
[Registry("render_input_module")]
public class RenderInputModuleRegistry : IRegistry
{
    /// <inheritdoc />
    public ushort RegistryId => RegistryIDs.RenderInputModule;

    /// <inheritdoc />
    public IEnumerable<ushort> RequiredRegistries => Enumerable.Empty<ushort>();

    /// <summary/>
    public required IInputModuleManager InputModuleManager { private get; [UsedImplicitly] set; }

    /// <summary>
    ///  Register a input module
    /// </summary>
    /// <param name="id"> Id of the input module</param>
    /// <typeparam name="TInputModule"> Type of the input module</typeparam>
    [RegisterMethod(ObjectRegistryPhase.Main)]
    public void RegisterInputDataModule<TInputModule>(Identification id) where TInputModule : InputModule
    {
        InputModuleManager.RegisterInputModule<TInputModule>(id);
    }


    /// <inheritdoc />
    public void UnRegister(Identification objectId)
    {
        InputModuleManager.UnRegisterInputModule(objectId);
    }

    /// <inheritdoc />
    public void Clear()
    {
        InputModuleManager.Clear();
    }
}