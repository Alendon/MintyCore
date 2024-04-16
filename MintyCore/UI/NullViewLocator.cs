using MintyCore.Utils;

namespace MintyCore.UI;

public class NullViewLocator : IViewLocator
{
    public void AddViewModel<TViewModel>(Identification id) where TViewModel : ViewModel
    {
        throw new System.NotSupportedException();
    }

    public void RemoveViewModel(Identification id)
    {
        throw new System.NotSupportedException();
    }

    public void AddView<TView>(Identification id, ViewDescription<TView> description) where TView : View, new()
    {
        throw new System.NotSupportedException();
    }

    public void RemoveView(Identification id)
    {
        throw new System.NotSupportedException();
    }

    public void ApplyChanges()
    {
        throw new System.NotSupportedException();
    }

    public ViewModel GetViewModel(Identification id)
    {
        throw new System.NotSupportedException();
    }

    public (ViewModel? ViewModel, View? View) GetOrCreateView(Identification viewId)
    {
        return (null, null);
    }

    public bool SetRootView(Identification id)
    {
        throw new System.NotSupportedException();
    }

    public ViewModel? GetRootViewModel()
    {
        throw new System.NotSupportedException();
    }

    public bool ClearRootView()
    {
        throw new System.NotSupportedException();
    }
}