using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
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
    private AdjacencyGraph<Identification, Edge<Identification>>? _inputGraph;
    private Dictionary<Identification, List<Identification>>? _inputDependencies;


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
        _inputGraph = null;
        _inputDependencies = null;

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

        var builder = new ContainerBuilder();
        foreach (var (_, renderInputBuilder) in _renderInputBuilders)
        {
            renderInputBuilder(builder);
        }

        _renderInputScope = builder.Build().BeginLifetimeScope();
        SortInputs();
    }

    private void SortInputs()
    {
        _inputGraph = new AdjacencyGraph<Identification, Edge<Identification>>();
        _inputDependencies = new Dictionary<Identification, List<Identification>>();
        foreach (var inputId in _renderInputBuilders.Keys)
        {
            _inputGraph.AddVertex(inputId);
            _inputDependencies.Add(inputId, new List<Identification>());
        }

        foreach (var inputId in _renderInputBuilders.Keys)
        {
            var input = GetRenderInput(inputId);

            foreach (var before in input.ExecuteAfter)
            {
                _inputGraph.AddEdge(new Edge<Identification>(before, inputId));
                _inputDependencies[inputId].Add(before);
            }

            foreach (var after in input.ExecuteBefore)
            {
                _inputGraph.AddEdge(new Edge<Identification>(inputId, after));
                _inputDependencies[after].Add(inputId);
            }
        }

        _inputGraph.TrimEdgeExcess();
        if (_inputGraph.IsDirectedAcyclicGraph())
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

    public Task ProcessAll()
    {
        if (_inputGraph is null || _inputDependencies is null)
        {
            Log.Error("Tried to process unprepared render inputs");
            return Task.CompletedTask;
        }

        Dictionary<Identification, Task> tasks = new(_renderInputBuilders.Count);

        foreach (var inputId in _inputGraph.TopologicalSort())
        {
            var input = GetRenderInput(inputId);
            var dependency = Task.CompletedTask;
            if (_inputDependencies[inputId].Count != 0)
            {
                var dependencyTasks = new List<Task>(_inputDependencies[inputId].Count);
                dependencyTasks.AddRange(_inputDependencies[inputId].Select(dependencyId => tasks[dependencyId]));

                dependency = Task.WhenAll(dependencyTasks);
            }

            tasks.Add(inputId, dependency.ContinueWith(_ => input.Process()));
        }

        return Task.WhenAll(tasks.Values);
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