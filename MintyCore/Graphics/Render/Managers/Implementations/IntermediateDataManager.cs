using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using MintyCore.Graphics.Render.Data;
using MintyCore.Graphics.Render.Data.RegistryWrapper;
using MintyCore.Utils;

namespace MintyCore.Graphics.Render.Managers.Implementations;

[Singleton<IIntermediateDataManager>(SingletonContextFlags.NoHeadless)]
internal class IntermediateDataManager : IIntermediateDataManager
{
    private readonly Dictionary<Identification, Func<IIntermediateDataManager, IntermediateData>>
        _intermediateDataRegistryCreators =
            new();
    private readonly Dictionary<Identification, Type> _intermediateDataTypes = new();

    private readonly Dictionary<Identification, ConcurrentQueue<IntermediateData>> _recycledIntermediateData =
        new();

    private readonly Dictionary<Identification, IntermediateData?> _currentData = new();

    public void RegisterIntermediateData<TIntermediateData>(Identification intermediateDataId)
        where TIntermediateData : IntermediateData, new()
    {
        _intermediateDataRegistryCreators.Add(intermediateDataId,
            manager => new TIntermediateData { IntermediateDataManager = manager });
        _recycledIntermediateData.Add(intermediateDataId, new ConcurrentQueue<IntermediateData>());
        
        _intermediateDataTypes.Add(intermediateDataId, typeof(TIntermediateData));
    }

    public void RegisterIntermediateData(Identification intermediateDataId,
        IntermediateDataRegistryWrapper registryWrapper)
    {
        _intermediateDataRegistryCreators.Add(intermediateDataId, registryWrapper.CreateIntermediateData);
        _recycledIntermediateData.Add(intermediateDataId, new ConcurrentQueue<IntermediateData>());
    }

    public IntermediateData GetNewIntermediateData(Identification intermediateDataId)
    {
        if (!_recycledIntermediateData[intermediateDataId].TryDequeue(out var data))
            data = _intermediateDataRegistryCreators[intermediateDataId](this);

        _currentData.TryGetValue(intermediateDataId, out var currentData);
        data.CopyFrom(currentData);

        return data;
    }

    public void SetCurrentData(Identification intermediateDataId, IntermediateData newData)
    {
        if (_currentData.TryGetValue(intermediateDataId, out var currentData))
            currentData?.DecreaseRefCount();

        _currentData[intermediateDataId] = newData;
        newData.IncreaseRefCount();
    }

    public IEnumerable<Identification> GetRegisteredIntermediateDataIds()
    {
        return _intermediateDataRegistryCreators.Keys;
    }

    public IntermediateData? GetCurrentData(Identification intermediateId)
    {
        return _currentData.TryGetValue(intermediateId, out var data) ? data : null;
    }

    public void UnRegisterIntermediateData(Identification objectId)
    {
        if (_currentData.Remove(objectId, out var current))
            current?.Dispose();

        _intermediateDataRegistryCreators.Remove(objectId);

        if (_recycledIntermediateData.Remove(objectId, out var recycled))
            while (recycled.TryDequeue(out var data))
                data.Dispose();
    }

    public void Clear()
    {
        foreach (var (_, current) in _currentData)
            current?.Dispose();

        _currentData.Clear();

        foreach (var (_, recycled) in _recycledIntermediateData)
            while (recycled.TryDequeue(out var data))
                data.Dispose();

        _recycledIntermediateData.Clear();
    }

    public Type GetIntermediateDataType(Identification intermediateDataId)
    {
        return _intermediateDataTypes[intermediateDataId];
    }

    public void RecycleIntermediateData(Identification intermediateDataId, IntermediateData data)
    {
        data.Clear();
        _recycledIntermediateData[intermediateDataId].Enqueue(data);
    }
}