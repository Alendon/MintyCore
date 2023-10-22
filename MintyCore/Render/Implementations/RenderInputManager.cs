using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using MintyCore.Utils;
using Serilog;

namespace MintyCore.Render.Implementations;

[Singleton<IRenderInputManager>(SingletonContextFlags.NoHeadless)]
internal sealed class RenderInputManager : IRenderInputManager, IAsyncDisposable
{
    private readonly Dictionary<Identification, Action<ContainerBuilder>> _renderInputBuilders = new();

    private ILifetimeScope? _renderInputScope;


    public void AddRenderInput<TRenderInput>(Identification renderInputId) where TRenderInput : IRenderInput
    {
        _renderInputBuilders.Add(
            renderInputId,
            builder =>
            {
                var registryBuilder = builder.RegisterType<TRenderInput>().Keyed<IRenderInput>(renderInputId)
                    .SingleInstance();

                var interfaces = typeof(TRenderInput).GetInterfaces();

                foreach (var i in interfaces)
                {
                    if (!i.IsGenericType) continue;

                    //TODO: Add support to make this modular, allowing different types of sub render inputs
                    if (i.GetGenericTypeDefinition() == typeof(IRenderInput<>))
                    {
                        registryBuilder = registryBuilder.Keyed(renderInputId, i);
                    }

                    if (i.GetGenericTypeDefinition() == typeof(IRenderInput<,>))
                    {
                        registryBuilder = registryBuilder.Keyed(renderInputId, i);
                    }
                }
            });
    }

    public void RemoveRenderInput(Identification renderInputId)
    {
        _renderInputScope?.Dispose();
        _renderInputScope = null;
        _renderInputBuilders.Remove(renderInputId);
    }

    public void ConstructRenderInputs()
    {
        if (_renderInputScope is not null)
        {
            Log.Warning("Tried to construct render input lifetime scope, while a scope was already constructed. Recreating scope");
            _renderInputScope.Dispose();
            _renderInputScope = null;
        }
        
        var builder = new ContainerBuilder();
        foreach (var (_, renderInputBuilder) in _renderInputBuilders)
        {
            renderInputBuilder(builder);
        }

        _renderInputScope = builder.Build().BeginLifetimeScope();
    }

    public void SetData<TKey, TValue>(Identification renderInputId, TKey key, TValue value)
    {
        GetRenderInput<TKey, TValue>(renderInputId).SetData(key, value);
    }

    public void RemoveData<TKey>(Identification renderInputId, TKey key)
    {
        GetRenderInput<TKey>(renderInputId).RemoveData(key);
    }

    public Task ProcessAll()
    {
        if (_renderInputScope is null) return Task.CompletedTask;

        List<Task> tasks = new();
        foreach (var renderInput in _renderInputScope.Resolve<IEnumerable<IRenderInput>>())
        {
            tasks.Add(renderInput.Process());
        }

        return Task.WhenAll(tasks);
    }

    public IRenderInput<TKey, TValue> GetRenderInput<TKey, TValue>(Identification renderInputId)
    {
        return _renderInputScope?.ResolveKeyed<IRenderInput<TKey, TValue>>(renderInputId) ??
               throw new MintyCoreException(
                   $"Tried to resolve render input with id {renderInputId} but the render input scope was null.");
    }

    public IRenderInput<TKey> GetRenderInput<TKey>(Identification renderInputId)
    {
        return _renderInputScope?.ResolveKeyed<IRenderInput<TKey>>(renderInputId) ??
               throw new MintyCoreException(
                   $"Tried to resolve render input with id {renderInputId} but the render input scope was null.");
    }

    public IRenderInput GetRenderInput(Identification renderInputId)
    {
        return _renderInputScope?.ResolveKeyed<IRenderInput>(renderInputId) ??
               throw new MintyCoreException(
                   $"Tried to resolve render input with id {renderInputId} but the render input scope was null.");
    }

    public void Dispose()
    {
        _renderInputScope?.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_renderInputScope != null) await _renderInputScope.DisposeAsync();
    }
}