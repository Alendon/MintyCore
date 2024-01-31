using System.Collections.Generic;
using System.Linq;
using MintyCore.Graphics.Render.Data;
using MintyCore.Graphics.Render.Managers;
using MintyCore.Utils;
using Serilog;

namespace MintyCore.Graphics.Render;

public class ModuleDataAccessor(IInputDataManager inputDataManager, IIntermediateDataManager intermediateDataManager)
{
    private readonly Dictionary<Identification, HashSet<Identification>> _inputDataConsumers = new();
    private readonly Dictionary<Identification, HashSet<Identification>> _intermediateDataConsumers = new();
    private readonly Dictionary<Identification, Identification> _intermediateDataProviders = new();

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

        if (!_intermediateDataConsumers.TryGetValue(intermediateData, out var value))
        {
            value = new HashSet<Identification>();
            _intermediateDataConsumers.Add(intermediateData, value);
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

        foreach (var (inputData, consumers) in _intermediateDataConsumers)
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
}