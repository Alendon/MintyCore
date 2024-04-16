using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MintyCore.UI;

public abstract partial class ViewModel : ObservableObject
{
    private Task? _loadTask;

    public event EventHandler? Closed;

    private INavigator? _navigator;
    protected INavigator Navigator => _navigator ?? throw new InvalidOperationException("ViewModel not loaded");

    public Task EnsureLoadedAsync(INavigator containingNavigator)
    {
        _navigator = containingNavigator;
        return _loadTask ??= LoadAsync();
    }

    protected abstract Task LoadAsync();

    [RelayCommand]
    public async Task<bool> TryCloseAsync() {
        if (!await TryCloseCoreAsync())
            return false;

        OnClosed();
        return true;
    }

    protected virtual Task<bool> TryCloseCoreAsync()
        => Task.FromResult(true);

    private void OnClosed()
        => Closed?.Invoke(this, EventArgs.Empty);

    public virtual void ProcessFrame() {
    }
}