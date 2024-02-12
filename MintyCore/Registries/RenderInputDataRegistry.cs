using System.Collections.Generic;
using System.Linq;
using MintyCore.Graphics.Render.Data.RegistryWrapper;
using MintyCore.Graphics.Render.Managers;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Modding.Attributes;
using MintyCore.Modding.Implementations;
using MintyCore.Utils;

namespace MintyCore.Registries;

/// <summary>
///   Registry to manage render input data
/// </summary>
[Registry("render_input_data")]
public class RenderInputDataRegistry : IRegistry
{
    /// <inheritdoc />
    public ushort RegistryId => RegistryIDs.RenderInputData;

    /// <inheritdoc />
    public IEnumerable<ushort> RequiredRegistries => Enumerable.Empty<ushort>();
    /// <summary/>
    public required IInputDataManager InputDataManager { private get; set; }
    
    /// <summary>
    /// Register a singleton input data
    /// </summary>
    /// <param name="id"> Id of the input data</param>
    /// <param name="wrapper"> Wrapper for the input data</param>
    [RegisterMethod(ObjectRegistryPhase.Main)]
    public void RegisterSingletonInputData(Identification id, SingletonInputDataRegistryWrapper wrapper)
    {
        InputDataManager.RegisterSingletonInputDataType(id, wrapper);
    }
    
    /// <summary>
    ///  Register a key indexed input data
    /// </summary>
    /// <param name="id"> Id of the input data</param>
    /// <param name="wrapper"> Wrapper for the input data</param>
    [RegisterMethod(ObjectRegistryPhase.Main)]
    public void RegisterKeyIndexedInputData(Identification id, DictionaryInputDataRegistryWrapper wrapper) 
    {
        InputDataManager.RegisterKeyIndexedInputDataType(id, wrapper);
    }

    /// <inheritdoc />
    public void UnRegister(Identification objectId)
    {
        InputDataManager.UnRegisterInputDataType(objectId);
    }

    /// <inheritdoc />
    public void Clear()
    {
        InputDataManager.Clear();
    }
}