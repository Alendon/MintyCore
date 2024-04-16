using System.Collections.Generic;
using System.Linq;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Modding.Attributes;
using MintyCore.Modding.Implementations;
using MintyCore.UI;
using MintyCore.Utils;

namespace MintyCore.Registries;

[Registry("view_model")]
public class ViewModelRegistry(IViewLocator viewLocator) : IRegistry
{
    public ushort RegistryId => RegistryIDs.ViewModel;
    public IEnumerable<ushort> RequiredRegistries => Enumerable.Empty<ushort>();
    
    [RegisterMethod(ObjectRegistryPhase.Main)]
    public void RegisterViewModel<TViewModel>(Identification id) where TViewModel : ViewModel
    {
        viewLocator.AddViewModel<TViewModel>(id);
    }

    public void PostRegister(ObjectRegistryPhase currentPhase)
    {
        if(currentPhase == ObjectRegistryPhase.Main)
        {
            viewLocator.ApplyChanges();
        }
    }

    public void UnRegister(Identification objectId)
    {
        viewLocator.RemoveViewModel(objectId);
    }

    public void Clear()
    {
    }
}