using System.Collections.Generic;
using System.Linq;
using MintyCore.Graphics.Render;
using MintyCore.Graphics.Render.Implementations;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Modding.Attributes;
using MintyCore.Modding.Implementations;
using MintyCore.Utils;

namespace MintyCore.Registries;

[Registry("render_input_data")]
public class RenderInputDataRegistry : IRegistry
{
    public ushort RegistryId => RegistryIDs.RenderInputData;
    public IEnumerable<ushort> RequiredRegistries => Enumerable.Empty<ushort>();
    
    public required IInputDataManager InputDataManager { private get; set; }
    
    [RegisterMethod(ObjectRegistryPhase.Main)]
    public void RegisterSingletonInputData(Identification id, SingletonInputDataRegistryWrapper wrapper)
    {
        InputDataManager.RegisterSingletonInputDataType(id, wrapper);
    }
    
    [RegisterMethod(ObjectRegistryPhase.Main)]
    public void RegisterKeyIndexedInputData(Identification id, DictionaryInputDataRegistryWrapper wrapper) 
    {
        InputDataManager.RegisterKeyIndexedInputDataType(id, wrapper);
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