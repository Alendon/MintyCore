using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia.Threading;
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

        _loadTask = null;
        OnClosed();
        return true;
    }

    protected virtual Task<bool> TryCloseCoreAsync()
        => Task.FromResult(true);

    private void OnClosed()
        => Closed?.Invoke(this, EventArgs.Empty);

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        if(Dispatcher.UIThread.CheckAccess())
            base.OnPropertyChanged(e);
        else
            Dispatcher.UIThread.Invoke(() => base.OnPropertyChanged(e));
    }

    protected override void OnPropertyChanging(PropertyChangingEventArgs e)
    {
        if(Dispatcher.UIThread.CheckAccess())
            base.OnPropertyChanging(e);
        else
            Dispatcher.UIThread.Invoke(() => base.OnPropertyChanging(e));
    }
}