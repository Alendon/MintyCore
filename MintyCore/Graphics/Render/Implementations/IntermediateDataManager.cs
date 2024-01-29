using System.Collections.Generic;
using MintyCore.Utils;

namespace MintyCore.Graphics.Render.Implementations;

[Singleton<IIntermediateDataManager>(SingletonContextFlags.NoHeadless)]
internal class IntermediateDataManager : IIntermediateDataManager
{
    public required IInputModuleManager InputModuleManager { private get; set; }
    
    private readonly Dictionary<Identification, IntermediateDataRegistryWrapper> _intermediateDataRegistryWrappers =
        new();
    private readonly Queue<IntermediateDataSet> _cachedIntermediateDataSets = new();
    private IntermediateDataSet? _currentIntermediateDataSet;
    
    private readonly Dictionary<Identification, Identification> _intermediateProvider = new();
    private readonly Dictionary<Identification, List<Identification>> _intermediateConsumerInputModule = new();

    private readonly object _lock = new();

    public void RegisterIntermediateData(Identification intermediateDataId,
        IntermediateDataRegistryWrapper intermediateDataRegistryWrapper)
    {
        _intermediateDataRegistryWrappers.Add(intermediateDataId, intermediateDataRegistryWrapper);
    }

    public IntermediateDataSet GetNewIntermediateDataSet()
    {
        lock (_lock)
        {
            if (!_cachedIntermediateDataSets.TryDequeue(out var result))
            {
                var intermediateData = new Dictionary<Identification, IntermediateData>();
                foreach (var (key, value) in _intermediateDataRegistryWrappers)
                {
                    intermediateData.Add(key, value.CreateIntermediateData());
                }

                result = new IntermediateDataSet(this, intermediateData);
            }

            result.IncreaseUseCount();
            return result;
        }
    }

    public void RecycleIntermediateDataSet(IntermediateDataSet intermediateDataSet)
    {
        lock (_lock)
            _cachedIntermediateDataSets.Enqueue(intermediateDataSet);
    }

    public void SetCurrentIntermediateDataSet(IntermediateDataSet intermediateSet)
    {
        lock (_lock)
        {
            _currentIntermediateDataSet?.DecreaseUseCount();
            _currentIntermediateDataSet = intermediateSet;
            _currentIntermediateDataSet.IncreaseUseCount();
        }
    }

    public IntermediateDataSet? GetCurrentIntermediateDataSet()
    {
        lock (_lock)
            return _currentIntermediateDataSet;
    }

    public void SetIntermediateProvider(Identification inputModuleId, Identification intermediateDataId)
    {
        if(!InputModuleManager.RegisteredInputModuleIds.Contains(inputModuleId))
            throw new MintyCoreException($"Input Module {inputModuleId} is not registered");
        
        _intermediateProvider.Add(intermediateDataId, inputModuleId);
    }

    public void SetIntermediateConsumerInputModule(Identification inputModuleId, Identification intermediateDataId)
    {
        if(!InputModuleManager.RegisteredInputModuleIds.Contains(inputModuleId))
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
            if(_intermediateProvider.ContainsKey(data)) continue;
            
            throw new MintyCoreException($"No intermediate data provider found for {data} (consumers: {consumer})");
        }
    }
}