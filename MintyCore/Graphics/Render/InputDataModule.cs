using System;
using System.Collections.Generic;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Graphics.Render;

public abstract class InputDataModule : IDisposable
{
    public abstract void Setup();
    public abstract void Update(CommandBuffer commandBuffer);
    public abstract Identification Identification { get; }
    
    public required IInputDataManager InputDataManager { protected get; set; }
    public required IIntermediateDataManager IntermediateDataManager { protected get; set; }
    public IntermediateDataSet? CurrentIntermediateDataSet { protected get; set; }
    private HashSet<Identification> _accessedIntermediateData = new();

    /// <inheritdoc />
    public abstract void Dispose();
    
    protected void ProvideIntermediateData(Identification intermediateDataId)
    {
        IntermediateDataManager.SetIntermediateProvider(Identification, intermediateDataId);
        _accessedIntermediateData.Add(intermediateDataId);
    }
    
    protected void ConsumeIntermediateData(Identification intermediateDataId)
    {
        IntermediateDataManager.SetIntermediateConsumerInputModule(Identification, intermediateDataId);
        _accessedIntermediateData.Add(intermediateDataId);
    }
    
    protected TIntermediateData GetCurrentIntermediateData<TIntermediateData>(Identification intermediateDataId) where TIntermediateData : IntermediateData
    {
        if (CurrentIntermediateDataSet is null)
        {
            throw new InvalidOperationException("Current intermediate data set is null");
        }
        
        if (!_accessedIntermediateData.Contains(intermediateDataId))
        {
            throw new InvalidOperationException($"Intermediate data with id {intermediateDataId} is not marked as accessed");
        }
        
        return (TIntermediateData) CurrentIntermediateDataSet.GetSubData(intermediateDataId);
    }
}