using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MintyCore.Utils;
using Serilog;

namespace MintyCore.UI;

[PublicAPI]
public abstract class ViewModelNavigator(IViewLocator viewLocator) : ViewModel, INavigator
{
    private readonly List<(Identification viewId, ViewModel viewModel, View view)> _viewStack = new();

    public Identification CurrentViewId => _viewStack.Count > 0 ? _viewStack[^1].viewId : Identification.Invalid;
    public ViewModel? CurrentViewModel => _viewStack.Count > 0 ? _viewStack[^1].viewModel : null;
    public View? CurrentView => _viewStack.Count > 0 ? _viewStack[^1].view : null;
    
    
    public async Task NavigateTo(Identification viewId, bool isChild = true)
    {
        if (!isChild)
        {
            await TryCloseCurrentAsync();
        }
        
        var (viewModel, view) = viewLocator.GetOrCreateView(viewId);

        if (viewModel is null || view is null)
        {
            Log.Error("Failed to get view and view model for {@ViewId}", viewId);
            return;
        }
        
        view.DataContext = viewModel;
        
        _ = viewModel.EnsureLoadedAsync(this);

        _viewStack.Add((viewId, viewModel, view));
        viewModel.Closed += OnViewModelClosed;
        OnPropertyChanged(nameof(CurrentViewModel));
        OnPropertyChanged(nameof(CurrentView));
        OnPropertyChanged(nameof(CurrentViewId));
        
        
        return;

        void OnViewModelClosed(object? sender, EventArgs e) {
            viewModel.Closed -= OnViewModelClosed;

            var isCurrent = CurrentViewModel == viewModel;
            _viewStack.Remove((viewId, viewModel, view));

            if (isCurrent)
            {
                OnPropertyChanged(nameof(CurrentViewModel));
                OnPropertyChanged(nameof(CurrentView));
                OnPropertyChanged(nameof(CurrentViewId));
            }
        }
    }
  
    protected override async Task<bool> TryCloseCoreAsync() {
        while (CurrentView is not null) {
            if (!await TryCloseCurrentAsync())
                return false;
        }
        
        return true;
    }
    
    public async Task<bool> TryCloseCurrentAsync()
        => CurrentViewModel is { } viewModel && await viewModel.TryCloseAsync();

    public abstract void Quit();
}