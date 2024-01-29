using System.Collections.Generic;
using MintyCore.Utils;

namespace MintyCore.Graphics.Render.Implementations;

[Singleton<IIntermediateManager>(SingletonContextFlags.NoHeadless)]
public class IntermediateManager : IIntermediateManager
{
    public required IInputManager InputManager { private get; set; }
    
    private readonly Dictionary<Identification, IntermediateDataRegistryWrapper> _intermediateDataRegistryWrappers =
        new();

    private readonly Queue<IntermediateDataSet> _cachedIntermediateDataSets = new();

    private IntermediateDataSet? _currentIntermediateDataSet;

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

    public void SetIntermediateProvider(Identification identification, Identification intermediateDataId)
    {
        throw new System.NotImplementedException();
    }

    public void SetIntermediateConsumerInputModule(Identification identification, Identification intermediateDataId)
    {
        throw new System.NotImplementedException();
    }

    public void ValidateIntermediateDataProvided()
    {
        throw new System.NotImplementedException();
    }
}