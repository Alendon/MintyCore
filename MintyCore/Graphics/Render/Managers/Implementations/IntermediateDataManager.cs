using System.Collections.Concurrent;
using System.Collections.Generic;
using MintyCore.Graphics.Render.Data;
using MintyCore.Graphics.Render.Data.RegistryWrapper;
using MintyCore.Utils;

namespace MintyCore.Graphics.Render.Managers.Implementations;

[Singleton<IIntermediateDataManager>(SingletonContextFlags.NoHeadless)]
internal class IntermediateDataManager : IIntermediateDataManager
{
    public required IInputModuleManager InputModuleManager { private get; set; }

    private readonly Dictionary<Identification, IntermediateDataRegistryWrapper> _intermediateDataRegistryWrappers =
        new();

    private readonly Dictionary<Identification, ConcurrentQueue<IntermediateData>> _recycledIntermediateData =
        new();

    private readonly Dictionary<Identification, Identification> _intermediateProvider = new();
    private readonly Dictionary<Identification, List<Identification>> _intermediateConsumerInputModule = new();

    private readonly Dictionary<Identification, IntermediateData?> _currentData = new();

    public void RegisterIntermediateData(Identification intermediateDataId,
        IntermediateDataRegistryWrapper intermediateDataRegistryWrapper)
    {
        _intermediateDataRegistryWrappers.Add(intermediateDataId, intermediateDataRegistryWrapper);
        _recycledIntermediateData.Add(intermediateDataId, new ConcurrentQueue<IntermediateData>());
    }

    public IntermediateData GetNewIntermediateData(Identification intermediateDataId)
    {
        if (!_recycledIntermediateData[intermediateDataId].TryDequeue(out var data))
            data = _intermediateDataRegistryWrappers[intermediateDataId].CreateIntermediateData();
        
        _currentData.TryGetValue(intermediateDataId, out var currentData);
        data.CopyFrom(currentData);
        
        return data;
    }

    public void SetCurrentData(Identification intermediateDataId, IntermediateData originalData)
    {
        _currentData[intermediateDataId] = originalData;
    }

    public void RecycleIntermediateData(Identification intermediateDataId, IntermediateData data)
    {
        data.Reset();
        _recycledIntermediateData[intermediateDataId].Enqueue(data);
    }

    public void SetIntermediateProvider(Identification inputModuleId, Identification intermediateDataId)
    {
        if (!InputModuleManager.RegisteredInputModuleIds.Contains(inputModuleId))
            throw new MintyCoreException($"Input Module {inputModuleId} is not registered");

        _intermediateProvider.Add(intermediateDataId, inputModuleId);
    }

    public void SetIntermediateConsumerInputModule(Identification inputModuleId, Identification intermediateDataId)
    {
        if (!InputModuleManager.RegisteredInputModuleIds.Contains(inputModuleId))
            throw new MintyCoreException($"Input Module {inputModuleId} is not registered");

        if (!_intermediateConsumerInputModule.TryGetValue(intermediateDataId, out var list))
        {
            list = new List<Identification>();
            _intermediateConsumerInputModule.Add(intermediateDataId, list);
        }

        list.Add(inputModuleId);
    }

    public void ValidateIntermediateDataProvided()
    {
        foreach (var (data, consumer) in _intermediateConsumerInputModule)
        {
            if (_intermediateProvider.ContainsKey(data)) continue;

            throw new MintyCoreException($"No intermediate data provider found for {data} (consumers: {consumer})");
        }
    }
}