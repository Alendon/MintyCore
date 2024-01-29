using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using MintyCore.Utils;

namespace MintyCore.Graphics.Render;

[PublicAPI]
public class IntermediateDataSet
{
    private readonly Dictionary<Identification, IntermediateData> _intermediateData;
    private readonly object _useCountLock = new();
    private int _useCount = 0;
    private AccessMode _accessMode;
    private IIntermediateDataManager _intermediateDataManager;

    public IntermediateDataSet(IIntermediateDataManager intermediateDataManager,
        Dictionary<Identification, IntermediateData> intermediateData)
    {
        _intermediateData = intermediateData;
        _intermediateDataManager = intermediateDataManager;
    }

    public AccessMode AccessMode
    {
        get => _accessMode;
        set
        {
            _accessMode = value;
            foreach (var dataValue in _intermediateData.Values)
            {
                dataValue.AccessMode = value;
            }
        }
    }

    public IntermediateData GetSubData(Identification id)
    {
        if (_useCount == 0)
            throw new InvalidOperationException("Data set is not in use");
        
        if (_intermediateData.TryGetValue(id, out var data))
            return data;

        throw new ArgumentException($"Intermediate data with id {id} not found");
    }

    public void Reset()
    {
        foreach (var dataValue in _intermediateData.Values)
        {
            dataValue.Reset();
        }

        _useCount = 0;
    }

    public void IncreaseUseCount()
    {
        lock (_useCountLock)
            _useCount++;
    }

    public void DecreaseUseCount(bool ignoreZero = false)
    {
        lock (_useCountLock)
        {
            if (_useCount == 0 && !ignoreZero)
                throw new InvalidOperationException("Use count is already 0");

            _useCount--;
            if (_useCount == 0)
            {
                Reset();
                _intermediateDataManager.RecycleIntermediateDataSet(this);
            }
        }
    }
}