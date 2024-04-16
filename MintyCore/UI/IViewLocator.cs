using MintyCore.Utils;

namespace MintyCore.UI;

public interface IViewLocator
{
    void AddViewModel<TViewModel>(Identification id) where TViewModel : ViewModel;
    void RemoveViewModel(Identification id);
    
    void AddView<TView>(Identification id, ViewDescription<TView> description) where TView : View,new();
    void RemoveView(Identification id);
    
    void ApplyChanges();
    
    ViewModel GetViewModel(Identification id);
    (ViewModel? ViewModel, View? View) GetOrCreateView(Identification viewId);

    /// <summary>
    /// Set the root view of the application
    /// </summary>
    /// <param name="id">The identification of the view to set as root</param>
    /// <remarks> The associated view model must implement <see cref="INavigator"/> </remarks>
    public bool SetRootView(Identification id);
    
    public ViewModel? GetRootViewModel();
    
    public bool ClearRootView();
}

public record struct ViewDescription<TView>(Identification ViewModelId) where TView : View, new();
