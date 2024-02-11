using System;
using System.Collections.Generic;
using System.Linq;
using MintyCore.Graphics.Render.Data;
using MintyCore.Graphics.Render.Managers;
using MintyCore.Graphics.VulkanObjects;
using MintyCore.Utils;
using OneOf;
using QuikGraph;
using QuikGraph.Algorithms;
using Serilog;
using Silk.NET.Vulkan;

namespace MintyCore.Graphics.Render;

public class ModuleDataAccessor(
    IInputDataManager inputDataManager,
    IIntermediateDataManager intermediateDataManager,
    IRenderDataManager renderDataManager)
    : IInputModuleDataAccessor, IRenderModuleDataAccessor
{
    private readonly Dictionary<Identification, HashSet<Identification>> _inputDataConsumers = new();
    private readonly Dictionary<Identification, HashSet<Identification>> _intermediateDataInputModuleConsumers = new();
    private readonly Dictionary<Identification, HashSet<Identification>> _intermediateDataRenderModuleConsumers = new();
    private readonly Dictionary<Identification, Identification> _intermediateDataProviders = new();

    private readonly Dictionary<Identification, IntermediateData?> _inputIntermediateData = new();

    private readonly Dictionary<Identification, OneOf<Identification, Swapchain>> _renderModuleColorAttachments = new();
    private readonly Dictionary<Identification, Identification> _renderModuleDepthStencilAttachments = new();
    private readonly Dictionary<Identification, List<Identification>> _renderModuleSampledTexturesAccessed = new();
    private readonly Dictionary<Identification, List<Identification>> _renderModuleStorageTexturesAccessed = new();

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

    public OneOf<Identification, Swapchain>? GetRenderModuleColorAttachment(Identification id)
    {
        return _renderModuleColorAttachments.TryGetValue(id, out var value) ? value : null;
    }

    public Identification? GetRenderModuleDepthStencilAttachment(Identification id)
    {
        return _renderModuleDepthStencilAttachments.TryGetValue(id, out var value) ? value : null;
    }

    public IEnumerable<Identification> GetRenderModuleSampledTexturesAccessed(Identification id)
    {
        return _renderModuleSampledTexturesAccessed.TryGetValue(id, out var value)
            ? value
            : Enumerable.Empty<Identification>();
    }

    public IEnumerable<Identification> GetRenderModuleStorageTexturesAccessed(Identification id)
    {
        return _renderModuleStorageTexturesAccessed.TryGetValue(id, out var value)
            ? value
            : Enumerable.Empty<Identification>();
    }

    #region IInputModuleDataAccessor

    public SingletonInputData<TInputData> UseSingletonInputData<TInputData>(Identification inputDataId,
        InputModule inputModule) where TInputData : notnull
    {
        var inputData = inputDataManager.GetSingletonInputData<TInputData>(inputDataId);

        if (!_inputDataConsumers.TryGetValue(inputDataId, out var value))
        {
            value = new HashSet<Identification>();
            _inputDataConsumers.Add(inputDataId, value);
        }

        value.Add(inputModule.Identification);

        return inputData;
    }

    public DictionaryInputData<TKey, TData> UseDictionaryInputData<TKey, TData>(Identification inputDataId,
        InputModule inputModule) where TKey : notnull
    {
        var inputData = inputDataManager.GetDictionaryInputData<TKey, TData>(inputDataId);

        if (!_inputDataConsumers.TryGetValue(inputDataId, out var value))
        {
            value = new HashSet<Identification>();
            _inputDataConsumers.Add(inputDataId, value);
        }

        value.Add(inputModule.Identification);

        return inputData;
    }

    public Func<TIntermediateData> UseIntermediateData<TIntermediateData>(Identification intermediateDataId,
        InputModule inputModule) where TIntermediateData : IntermediateData
    {
        if (!intermediateDataManager.GetRegisteredIntermediateDataIds().Contains(intermediateDataId))
            throw new MintyCoreException($"Intermediate data {intermediateDataId} does not exist.");

        var registeredType = intermediateDataManager.GetIntermediateDataType(intermediateDataId);

        if (!registeredType.IsAssignableTo(typeof(TIntermediateData)))
        {
            throw new MintyCoreException(
                $"Registered intermediate data {intermediateDataId} ({registeredType.FullName}) is not compatible with {typeof(TIntermediateData).FullName}.");
        }

        if (!_intermediateDataInputModuleConsumers.TryGetValue(intermediateDataId, out var value))
        {
            value = new HashSet<Identification>();
            _intermediateDataInputModuleConsumers.Add(intermediateDataId, value);
        }

        value.Add(inputModule.Identification);

        //return a function that gets the newly created intermediate data this frame or from the previous
        return () => GetIntermediateDataInputModule<TIntermediateData>(intermediateDataId);
    }

    private TIntermediateData GetIntermediateDataInputModule<TIntermediateData>(Identification intermediateId)
        where TIntermediateData : IntermediateData
    {
        if (!_inputIntermediateData.TryGetValue(intermediateId, out var intermediateData))
        {
            intermediateData = intermediateDataManager.GetCurrentData(intermediateId);

            if (intermediateData is null)
                throw new MintyCoreException(
                    $"Intermediate data {intermediateId} was not provided this frame, and no previous data was found.");

            intermediateData.IncreaseRefCount();

            _inputIntermediateData.Add(intermediateId, intermediateData);
        }

        if (intermediateData is not TIntermediateData castedData)
            throw new MintyCoreException(
                $"Intermediate data {intermediateId} is not of type {typeof(TIntermediateData)}.");

        return castedData;
    }

    public Func<TIntermediateData?> UseIntermediateData<TIntermediateData>(Identification intermediateDataId,
        RenderModule renderModule) where TIntermediateData : IntermediateData
    {
        if (!intermediateDataManager.GetRegisteredIntermediateDataIds().Contains(intermediateDataId))
            throw new MintyCoreException($"Intermediate data {intermediateDataId} does not exist.");

        var registeredType = intermediateDataManager.GetIntermediateDataType(intermediateDataId);

        if (!registeredType.IsAssignableTo(typeof(TIntermediateData)))
        {
            throw new MintyCoreException(
                $"Registered intermediate data {intermediateDataId} ({registeredType.FullName}) is not compatible with {typeof(TIntermediateData).FullName}.");
        }

        if (!_intermediateDataRenderModuleConsumers.TryGetValue(intermediateDataId, out var value))
        {
            value = new HashSet<Identification>();
            _intermediateDataRenderModuleConsumers.Add(intermediateDataId, value);
        }

        value.Add(renderModule.Identification);

        //return a function that gets the newly created intermediate data this frame or from the previous
        return () => GetIntermediateDataRenderModule<TIntermediateData>(intermediateDataId);
    }

    private TIntermediateData? GetIntermediateDataRenderModule<TIntermediateData>(Identification intermediateId)
        where TIntermediateData : IntermediateData
    {
        var data = intermediateDataManager.GetCurrentData(intermediateId);

        if (data is null) return null;

        if (data is not TIntermediateData castedData)
            throw new MintyCoreException(
                $"Intermediate data {intermediateId} is not of type {typeof(TIntermediateData)}.");

        return castedData;
    }

    public Func<TIntermediateData> ProvideIntermediateData<TIntermediateData>(Identification intermediateDataId,
        InputModule inputModule) where TIntermediateData : IntermediateData
    {
        if (!intermediateDataManager.GetRegisteredIntermediateDataIds().Contains(intermediateDataId))
            throw new MintyCoreException($"Intermediate data {intermediateDataId} does not exist.");

        Type registeredType = intermediateDataManager.GetIntermediateDataType(intermediateDataId);

        if (!registeredType.IsAssignableTo(typeof(TIntermediateData)))
        {
            throw new MintyCoreException(
                $"Registered intermediate data {intermediateDataId} ({registeredType.FullName}) is not compatible with {typeof(TIntermediateData).FullName}.");
        }

        if (_intermediateDataProviders.TryGetValue(intermediateDataId, out var value))
            throw new MintyCoreException(
                $"Intermediate data {intermediateDataId} already has a provider. (Current: {value}, New: {inputModule.Identification})");

        _intermediateDataProviders.Add(intermediateDataId, inputModule.Identification);

        //return a function that creates the intermediate data
        return () => CreateIntermediateData<TIntermediateData>(intermediateDataId);
    }

    private TIntermediateData CreateIntermediateData<TIntermediateData>(Identification intermediateId)
        where TIntermediateData : IntermediateData
    {
        var intermediateData = intermediateDataManager.GetNewIntermediateData(intermediateId);
        intermediateData.IncreaseRefCount();

        _inputIntermediateData.Add(intermediateId, intermediateData);

        return (TIntermediateData)intermediateData;
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

        foreach (var (inputData, consumers) in _intermediateDataRenderModuleConsumers)
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

    #endregion

    #region IRenderModuleDataAccessor

    public void SetColorAttachment(OneOf<Identification, Swapchain> targetTexture, RenderModule renderModule)
    {
        if (targetTexture.TryPickT0(out var textureId, out _))
        {
            var textureDescription = renderDataManager.GetRenderTextureDescription(textureId);

            if ((textureDescription.Usage & TextureUsage.DepthStencil) != 0)
                throw new MintyCoreException("DepthStencil usage is not allowed for color attachments");
        }

        if (targetTexture.TryPickT0(out textureId, out _))
        {
            if (_renderModuleSampledTexturesAccessed.TryGetValue(renderModule.Identification,
                    out var accessedSampledTextures) && accessedSampledTextures.Contains(textureId))
                throw new MintyCoreException(
                    "Texture cannot be used as a color attachment and a sampled texture at the same time");

            if (_renderModuleStorageTexturesAccessed.TryGetValue(renderModule.Identification,
                    out var accessedStorageTextures) && accessedStorageTextures.Contains(textureId))
                throw new MintyCoreException(
                    "Texture cannot be used as a color attachment and a storage texture at the same time");
        }

        _renderModuleColorAttachments[renderModule.Identification] = targetTexture;
    }

    public void SetDepthStencilAttachment(Identification targetDepthTexture, RenderModule renderModule)
    {
        var textureDescription = renderDataManager.GetRenderTextureDescription(targetDepthTexture);

        if ((textureDescription.Usage & TextureUsage.DepthStencil) == 0)
            throw new MintyCoreException("Target texture is not a depth stencil texture");


        _renderModuleDepthStencilAttachments[renderModule.Identification] = targetDepthTexture;
    }

    public Func<DescriptorSet> UseSampledTexture(Identification textureId, RenderModule renderModule)
    {
        var textureDescription = renderDataManager.GetRenderTextureDescription(textureId);

        if ((textureDescription.Usage & TextureUsage.DepthStencil) != 0)
            throw new MintyCoreException("Depth Texture is not allowed for sampled textures");

        if (_renderModuleStorageTexturesAccessed.TryGetValue(renderModule.Identification,
                out var accessedStorageTextures) && accessedStorageTextures.Contains(textureId))
            throw new MintyCoreException(
                "Texture cannot be used as a sampled texture and a storage texture at the same time");

        if (_renderModuleColorAttachments.TryGetValue(renderModule.Identification, out var colorAttachment) &&
            colorAttachment.TryPickT0(out var colorAttachmentId, out _) && colorAttachmentId == textureId)
            throw new MintyCoreException(
                "Texture cannot be used as a sampled texture and a color attachment at the same time");

        if (!_renderModuleSampledTexturesAccessed.TryGetValue(renderModule.Identification, out var value))
        {
            value = new List<Identification>();
            _renderModuleSampledTexturesAccessed.Add(renderModule.Identification, value);
        }

        value.Add(textureId);

        return () => renderDataManager.GetSampledTextureDescriptorSet(textureId);
    }

    public Func<DescriptorSet> UseStorageTexture(Identification textureId, RenderModule renderModule)
    {
        var textureDescription = renderDataManager.GetRenderTextureDescription(textureId);

        if ((textureDescription.Usage & TextureUsage.DepthStencil) != 0)
            throw new MintyCoreException("Depth Texture is not allowed for storage textures");

        if (_renderModuleSampledTexturesAccessed.TryGetValue(renderModule.Identification,
                out var accessedSampledTextures) && accessedSampledTextures.Contains(textureId))
            throw new MintyCoreException(
                "Texture cannot be used as a storage texture and a sampled texture at the same time");

        if (_renderModuleColorAttachments.TryGetValue(renderModule.Identification, out var colorAttachment) &&
            colorAttachment.TryPickT0(out var colorAttachmentId, out _) && colorAttachmentId == textureId)
            throw new MintyCoreException(
                "Texture cannot be used as a storage texture and a color attachment at the same time");

        if (!_renderModuleStorageTexturesAccessed.TryGetValue(renderModule.Identification, out var value))
        {
            value = new List<Identification>();
            _renderModuleStorageTexturesAccessed.Add(renderModule.Identification, value);
        }

        value.Add(textureId);

        return () => renderDataManager.GetStorageTextureDescriptorSet(textureId);
    }

    #endregion
}