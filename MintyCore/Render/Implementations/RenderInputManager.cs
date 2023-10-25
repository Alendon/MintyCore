using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using JetBrains.Annotations;
using MintyCore.Utils;
using QuikGraph;
using QuikGraph.Algorithms;
using Serilog;

namespace MintyCore.Render.Implementations;

[Singleton<IRenderInputManager>(SingletonContextFlags.NoHeadless)]
internal sealed class RenderInputManager : IRenderInputManager, IAsyncDisposable
{
    private readonly Dictionary<Identification, Action<ContainerBuilder>> _renderInputBuilders = new();

    private ILifetimeScope? _renderInputScope;
    
    public required ILifetimeScope LifetimeScope { private get; [UsedImplicitly] init; }

    public void AddRenderInput<TRenderInput>(Identification renderInputId) where TRenderInput : IRenderInput
    {
        _renderInputBuilders.Add(
            renderInputId,
            builder =>
            {
                var registryBuilder = builder.RegisterType<TRenderInput>().Keyed<IRenderInput>(renderInputId)
                    .SingleInstance().PropertiesAutowired();

                var interfaces = typeof(TRenderInput).GetInterfaces();

                foreach (var i in interfaces)
                {
                    if (!i.IsGenericType) continue;

                    //TODO: Add support to make this modular, allowing different types of sub render inputs
                    if (i.GetGenericTypeDefinition() == typeof(IRenderInputKey<>))
                    {
                        registryBuilder = registryBuilder.Keyed(renderInputId, i);
                    }

                    if (i.GetGenericTypeDefinition() == typeof(IRenderInputKeyValue<,>))
                    {
                        registryBuilder = registryBuilder.Keyed(renderInputId, i);
                    }

                    if (i.GetGenericTypeDefinition() == typeof(IRenderInputConcreteResult<>))
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
            Log.Warning(
                "Tried to construct render input lifetime scope, while a scope was already constructed. Recreating scope");
            _renderInputScope.Dispose();
            _renderInputScope = null;
        }

        _renderInputScope = LifetimeScope.BeginLifetimeScope(builder =>
        {
            foreach (var renderInputBuilder in _renderInputBuilders)
            {
                renderInputBuilder.Value(builder);
            }
        });
        
        ValidateInputs();
    }

    private void ValidateInputs()
    {
        var inputGraph = new AdjacencyGraph<Identification, Edge<Identification>>();
        var inputDependencies = new Dictionary<Identification, List<Identification>>();
        foreach (var inputId in _renderInputBuilders.Keys)
        {
            inputGraph.AddVertex(inputId);
            inputDependencies.Add(inputId, new List<Identification>());
        }

        foreach (var inputId in _renderInputBuilders.Keys)
        {
            var input = GetRenderInput(inputId);

            foreach (var before in input.ExecuteAfter)
            {
                inputGraph.AddEdge(new Edge<Identification>(before, inputId));
                inputDependencies[inputId].Add(before);
            }

            foreach (var after in input.ExecuteBefore)
            {
                inputGraph.AddEdge(new Edge<Identification>(inputId, after));
                inputDependencies[after].Add(inputId);
            }
        }

        inputGraph.TrimEdgeExcess();
        if (!inputGraph.IsDirectedAcyclicGraph())
        {
            throw new MintyCoreException("Circular dependency detected in render inputs.");
        }
    }


    public void SetData<TKey, TValue>(Identification renderInputId, TKey key, TValue value)
    {
        GetRenderInput<TKey, TValue>(renderInputId).SetData(key, value);
    }

    public void RemoveData<TKey>(Identification renderInputId, TKey key)
    {
        GetRenderInput<TKey>(renderInputId).RemoveData(key);
    }

    /// <inheritdoc />
    public void RecreateGpuData()
    {
        if (_renderInputScope is null)
            throw new MintyCoreException("Tried to recreate gpu data, but the render input scope was null.");

        foreach (var id in _renderInputBuilders.Keys)
        {
            var input = GetRenderInput(id);
            input.RecreateGpuData();
        }
    }

    public IRenderInputKeyValue<TKey, TValue> GetRenderInput<TKey, TValue>(Identification renderInputId)
    {
        return _renderInputScope?.ResolveKeyed<IRenderInputKeyValue<TKey, TValue>>(renderInputId) ??
               throw new MintyCoreException(
                   $"Tried to resolve render input with id {renderInputId} but the render input scope was null.");
    }

    public IRenderInputKey<TKey> GetRenderInput<TKey>(Identification renderInputId)
    {
        return _renderInputScope?.ResolveKeyed<IRenderInputKey<TKey>>(renderInputId) ??
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