using System.Collections.Concurrent;
using System.Collections.Generic;
using MintyCore.Graphics.Render.Data;
using MintyCore.Graphics.Render.Data.RegistryWrapper;
using MintyCore.Utils;

namespace MintyCore.Graphics.Render.Managers.Implementations;

[Singleton<IIntermediateDataManager>(SingletonContextFlags.NoHeadless)]
internal class IntermediateDataManager : IIntermediateDataManager
{
    private readonly Dictionary<Identification, IntermediateDataRegistryWrapper> _intermediateDataRegistryWrappers =
        new();

    private readonly Dictionary<Identification, ConcurrentQueue<IntermediateData>> _recycledIntermediateData =
        new();

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
            data = _intermediateDataRegistryWrappers[intermediateDataId].CreateIntermediateData(this);
        
        _currentData.TryGetValue(intermediateDataId, out var currentData);
        data.CopyFrom(currentData);
        
        return data;
    }

    public void SetCurrentData(Identification intermediateDataId, IntermediateData newData)
    {
        if(_currentData.TryGetValue(intermediateDataId, out var currentData))
            currentData?.DecreaseRefCount();
        
        _currentData[intermediateDataId] = newData;
        newData.IncreaseRefCount();
    }

    public IEnumerable<Identification> GetRegisteredIntermediateDataIds()
    {
        return _intermediateDataRegistryWrappers.Keys;
    }

    public IntermediateData? GetCurrentData(Identification intermediateId)
    {
        return _currentData.TryGetValue(intermediateId, out var data) ? data : null;
    }

    public void RecycleIntermediateData(Identification intermediateDataId, IntermediateData data)
    {
        data.Clear();
        _recycledIntermediateData[intermediateDataId].Enqueue(data);
    }
}