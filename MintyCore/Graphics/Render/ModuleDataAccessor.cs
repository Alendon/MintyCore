using System.Collections.Generic;
using System.Linq;
using MintyCore.Graphics.Render.Data;
using MintyCore.Graphics.Render.Managers;
using MintyCore.Utils;
using QuikGraph;
using QuikGraph.Algorithms;
using Serilog;

namespace MintyCore.Graphics.Render;

public class ModuleDataAccessor(IInputDataManager inputDataManager, IIntermediateDataManager intermediateDataManager)
    : IModuleDataAccessor
{
    private readonly Dictionary<Identification, HashSet<Identification>> _inputDataConsumers = new();
    private readonly Dictionary<Identification, HashSet<Identification>> _intermediateDataInputModuleConsumers = new();
    private readonly Dictionary<Identification, Identification> _intermediateDataProviders = new();
    
    private readonly Dictionary<Identification, IntermediateData?> _inputIntermediateData = new();

    public void SetInputDataConsumer(Identification inputData, Identification consumer)
    {
        if (!inputDataManager.GetRegisteredInputDataIds().Contains(inputData))
            throw new MintyCoreException($"Input data {inputData} does not exist.");

        if (!_inputDataConsumers.TryGetValue(inputData, out var value))
        {
            value = new HashSet<Identification>();
            _inputDataConsumers.Add(inputData, value);
        }

        value.Add(consumer);
    }

    public void SetIntermediateDataConsumer(Identification intermediateData, Identification consumer)
    {
        if (!intermediateDataManager.GetRegisteredIntermediateDataIds().Contains(intermediateData))
            throw new MintyCoreException($"Intermediate data {intermediateData} does not exist.");

        if (!_intermediateDataInputModuleConsumers.TryGetValue(intermediateData, out var value))
        {
            value = new HashSet<Identification>();
            _intermediateDataInputModuleConsumers.Add(intermediateData, value);
        }

        value.Add(consumer);
    }

    public void SetIntermediateDataProvider(Identification intermediateDataId, Identification inputModuleId)
    {
        if (!intermediateDataManager.GetRegisteredIntermediateDataIds().Contains(intermediateDataId))
            throw new MintyCoreException($"Intermediate data {intermediateDataId} does not exist.");

        if (_intermediateDataProviders.TryGetValue(intermediateDataId, out var value))
            throw new MintyCoreException(
                $"Intermediate data {intermediateDataId} already has a provider. (Current: {value}, New: {inputModuleId})");

        _intermediateDataProviders.Add(intermediateDataId, inputModuleId);
    }

    public void ValidateIntermediateDataProvided()
    {
        var touched = new HashSet<Identification>();

        foreach (var (inputData, consumers) in _intermediateDataInputModuleConsumers)
        {
            if (!_intermediateDataProviders.ContainsKey(inputData))
                throw new MintyCoreException(
                    $"No intermediate data provider found for {inputData} (consumers: {consumers})");

            touched.Add(inputData);
        }

        var untouched = _intermediateDataProviders.Keys.Except(touched).ToList();

        if (untouched.Count != 0)
            Log.Warning("Intermediate data {IntermediateData} is provided but not consumed", untouched);
    }

    public SingletonInputData<TData> GetSingletonInputData<TData>(Identification inputDataId,
        Identification inputModuleId) where TData : notnull
    {
        if (!_inputDataConsumers.TryGetValue(inputDataId, out var consumers) || !consumers.Contains(inputModuleId))
            throw new MintyCoreException($"Input module {inputModuleId} is not set as consumer for {inputDataId}");

        return inputDataManager.GetSingletonInputData<TData>(inputDataId);
    }

    public IReadOnlyList<Identification> SortInputModules(IEnumerable<Identification> inputModules)
    {
        var sortGraph = new AdjacencyGraph<Identification, Edge<Identification>>();

        sortGraph.AddVertexRange(inputModules);

        foreach (var (dataId, fromModule) in _intermediateDataProviders)
        {
            if (!_intermediateDataInputModuleConsumers.TryGetValue(dataId, out var consumers)) continue;

            foreach (var toModule in consumers)
            {
                sortGraph.AddEdge(new Edge<Identification>(fromModule, toModule));
            }
        }

        return sortGraph.TopologicalSort().ToList();
    }

    public IEnumerable<Identification> GetInputModuleConsumedInputDataIds(Identification id)
    {
        return _inputDataConsumers.Where(x => x.Value.Contains(id)).Select(x => x.Key);
    }

    public IEnumerable<Identification> GetInputModuleConsumedIntermediateDataIds(Identification id)
    {
        return _intermediateDataInputModuleConsumers.Where(x => x.Value.Contains(id)).Select(x => x.Key);
    }

    public IEnumerable<Identification> GetInputModuleProvidedIntermediateDataIds(Identification id)
    {
        return _intermediateDataProviders.Where(x => x.Value == id).Select(x => x.Key);
    }

    public void CreateNewIntermediateData(IReadOnlyList<Identification> providedIntermediateData)
    {
        foreach (var intermediateId in providedIntermediateData)
        {
            var intermediateData = intermediateDataManager.GetNewIntermediateData(intermediateId);
            intermediateData.IncreaseRefCount();
            
            _inputIntermediateData.Add(intermediateId, intermediateData);
        }
    }

    public void MakeIntermediateDataAvailable(IReadOnlyList<Identification> consumedIntermediateData)
    {
        foreach (var intermediateId in consumedIntermediateData)
        {
            if (_inputIntermediateData.ContainsKey(intermediateId)) continue;

            var currentData = intermediateDataManager.GetCurrentData(intermediateId);
            currentData?.IncreaseRefCount();
            
            _inputIntermediateData.Add(intermediateId, currentData);
        }
    }

    public void UpdateIntermediateData()
    {
        foreach (var (intermediateId, intermediateData) in _inputIntermediateData)
        {
            if (intermediateData is null) continue;
            
            intermediateDataManager.SetCurrentData(intermediateId, intermediateData);
            intermediateData.DecreaseRefCount();
        }
        
        _inputIntermediateData.Clear();
    }

    public void Clear()
    {
        //TODO implement when render modules are implemented
    }
}