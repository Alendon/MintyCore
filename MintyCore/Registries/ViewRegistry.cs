using System.Collections.Generic;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Modding.Attributes;
using MintyCore.Modding.Implementations;
using MintyCore.UI;
using MintyCore.Utils;

namespace MintyCore.Registries;

[Registry("view")]
public class ViewRegistry(IViewLocator viewLocator) : IRegistry
{
    public ushort RegistryId => RegistryIDs.View;
    public IEnumerable<ushort> RequiredRegistries => [RegistryIDs.ViewModel];
    
    [RegisterMethod(ObjectRegistryPhase.Main)]
    public void RegisterView<TView>(Identification id, ViewDescription<TView> viewDescription) where TView : View, new()
    {
        viewLocator.AddView(id, viewDescription);
    }

    public void UnRegister(Identification objectId)
    {
        viewLocator.RemoveView(objectId);
    }

    public void Clear()
    {
        
    }
}