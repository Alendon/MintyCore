using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Avalonia.Threading;
using MintyCore.AvaloniaIntegration;
using MintyCore.Utils;

namespace MintyCore.UI;

[Singleton<IViewLocator>]
internal sealed class ViewLocator(ILifetimeScope lifetimeScope, IAvaloniaController avaloniaController) : IViewLocator
{
    private readonly Dictionary<Identification, Action<ContainerBuilder>> _viewModelContainerBuilders = new();
    private readonly Dictionary<Identification, ViewModel> _viewModels = new();
    private readonly HashSet<Identification> _navigatorViewModels = new();
    private readonly Dictionary<ILifetimeScope, HashSet<Identification>> _lifetimeScopeViewModels = new();

    private readonly Dictionary<Identification, Func<View>> _viewBuilders = new();
    private readonly Dictionary<Identification, Identification> _viewToViewModel = new();

    //the current root view
    private (ViewModel? ViewModel, View? View) _rootView = (null, null);

    public void AddViewModel<TViewModel>(Identification id) where TViewModel : ViewModel
    {
        _viewModelContainerBuilders.Add(id,
            builder => builder.RegisterType<TViewModel>().Keyed<ViewModel>(id).SingleInstance());

        if (typeof(INavigator).IsAssignableFrom(typeof(TViewModel)))
        {
            _navigatorViewModels.Add(id);
        }
    }

    public void RemoveViewModel(Identification id)
    {
        _viewModelContainerBuilders.Remove(id);
        _viewModels.Remove(id);

        foreach (var (_, set) in _lifetimeScopeViewModels)
        {
            set.Remove(id);
        }
    }

    public void RemoveView(Identification id)
    {
        _viewBuilders.Remove(id);
    }

    public void ApplyChanges()
    {
        CreateMissingViewModels();
        CleanupRemovedViewModels();
    }

    private void CleanupRemovedViewModels()
    {
        var toRemove = _lifetimeScopeViewModels.Where(pair => pair.Value.Count == 0).ToArray();

        foreach (var (scope, _) in toRemove)
        {
            _lifetimeScopeViewModels.Remove(scope);
            scope.Dispose();
        }
    }

    private void CreateMissingViewModels()
    {
        // iterate over all view model ids which are not created yet
        var viewModelIds = _viewModelContainerBuilders.Keys.Except(_viewModels.Keys).ToArray();

        Action<ContainerBuilder> builder = _ => { };

        foreach (var id in viewModelIds)
        {
            builder += _viewModelContainerBuilders[id];
        }

        var container = lifetimeScope.BeginLifetimeScope(builder);

        foreach (var id in viewModelIds)
        {
            _viewModels.Add(id, container.ResolveKeyed<ViewModel>(id));
        }

        _lifetimeScopeViewModels.Add(container, viewModelIds.ToHashSet());
    }

    public void AddView<TView>(Identification id, ViewDescription<TView> description) where TView : View, new()
    {
        _viewBuilders.Add(id, () => new TView());
        _viewToViewModel.Add(id, description.ViewModelId);
    }

    public ViewModel GetViewModel(Identification id)
    {
        if (_viewModels.TryGetValue(id, out var viewModel))
        {
            return viewModel;
        }

        throw new InvalidOperationException($"ViewModel with id {id} not found");
    }

    public (ViewModel ViewModel, View View) GetOrCreateView(Identification viewId)
    {
        var viewModelId = _viewToViewModel[viewId];

        return (_viewModels[viewModelId], _viewBuilders[viewId]());
    }

    public bool SetRootView(Identification id)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            return Dispatcher.UIThread.Invoke(() => SetRootView(id));
        }
        
        var viewModelId = _viewToViewModel[id];

        if (!_navigatorViewModels.Contains(viewModelId))
            throw new InvalidOperationException($"ViewModel with id {viewModelId} does not implement INavigator");

        if (!ClearRootView())
            return false;

        var (viewModel, view) = GetOrCreateView(id);

        view.DataContext = viewModel;

        if (viewModel is not INavigator viewModelNavigator)
            throw new InvalidOperationException(
                $"ViewModel with id {id} does not implement INavigator. This should not happen");

        viewModel.EnsureLoadedAsync(viewModelNavigator).Wait();

        _rootView = (viewModel, view);
        
        avaloniaController.TopLevel.Content = view;


        return true;
    }

    public ViewModel? GetRootViewModel()
    {
        return _rootView.ViewModel;
    }

    public bool ClearRootView()
    {
        if (!Dispatcher.UIThread.CheckAccess())
            return Dispatcher.UIThread.Invoke(ClearRootView);
        
        if (_rootView.ViewModel is { } viewModel)
        {
            var task = viewModel.TryCloseAsync();
            task.Wait();

            if (!task.Result)
            {
                return false;
            }
        }

        _rootView = (null, null);

        avaloniaController.TopLevel.Content = null;

        return true;
    }
}