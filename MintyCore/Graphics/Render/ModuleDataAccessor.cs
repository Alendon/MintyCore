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

/// <summary>
/// Class for managing data access for input and render modules.
/// </summary>
public class ModuleDataAccessor(
    IInputDataManager inputDataManager,
    IIntermediateDataManager intermediateDataManager,
    IRenderDataManager renderDataManager)
    : IInputModuleDataAccessor, IRenderModuleDataAccessor
{
    // Various dictionaries for managing data consumers and providers.
    private readonly Dictionary<Identification, HashSet<Identification>> _inputDataConsumers = new();
    private readonly Dictionary<Identification, HashSet<Identification>> _intermediateDataInputModuleConsumers = new();
    private readonly Dictionary<Identification, HashSet<Identification>> _intermediateDataRenderModuleConsumers = new();
    private readonly Dictionary<Identification, Identification> _intermediateDataProviders = new();

    // Dictionary for managing intermediate data.
    private readonly Dictionary<Identification, IntermediateData?> _inputIntermediateData = new();

    // All intermediate data used by render modules. Associated by the current swapchain image index.
    private readonly Dictionary<uint, List<IntermediateData>> _renderModuleIntermediateData = new();
    private uint _currentSwapchainImageIndex;

    // Dictionaries for managing render module attachments and accessed textures.
    private readonly Dictionary<Identification, List<OneOf<Identification, Swapchain>>> _renderModuleColorAttachments =
        new();

    private readonly Dictionary<Identification, Identification> _renderModuleDepthStencilAttachments = new();
    private readonly Dictionary<Identification, List<Identification>> _renderModuleSampledTexturesAccessed = new();
    private readonly Dictionary<Identification, List<Identification>> _renderModuleStorageTexturesAccessed = new();

    /// <summary>
    /// Updates the intermediate data for the current frame.
    /// </summary>
    public void UpdateIntermediateData()
    {
        foreach (var (intermediateId, intermediateData) in _inputIntermediateData)
        {
            if (intermediateData is null) continue;
            intermediateDataManager.SetCurrentData(intermediateId, intermediateData);
            //ref count is not changed, as it moves directly from the working data to the current data
        }

        _inputIntermediateData.Clear();
    }


    /// <summary>
    /// Set the current swapchain image index. Clears the used intermediate data for the previous frame on the same swapchain image index.
    /// </summary>
    public void SetCurrentFrameIndex(uint vulkanEngineSwapchainImageIndex)
    {
        if (!_renderModuleIntermediateData.ContainsKey(vulkanEngineSwapchainImageIndex))
            _renderModuleIntermediateData.Add(vulkanEngineSwapchainImageIndex, new List<IntermediateData>());

        var usedData = _renderModuleIntermediateData[vulkanEngineSwapchainImageIndex];

        foreach (var data in usedData)
        {
            data.DecreaseRefCount();
        }

        usedData.Clear();

        _currentSwapchainImageIndex = vulkanEngineSwapchainImageIndex;
    }

    public void ClearUsedIntermediateData()
    {
        foreach (var (_, usedData) in _renderModuleIntermediateData)
        {
            foreach (var data in usedData)
            {
                data.DecreaseRefCount();
            }

            usedData.Clear();
        }
    }

    /// <summary>
    /// Sorts the input modules based on their dependencies.
    /// </summary>
    /// <param name="inputModules">The input modules to sort.</param>
    /// <returns>A sorted list of input modules.</returns>
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

    /// <summary>
    /// Gets the input data consumed by the specified input module.
    /// </summary>
    /// <param name="id">The identification of the input module.</param>
    /// <returns>The consumed input data.</returns>
    public IEnumerable<Identification> GetInputModuleConsumedInputDataIds(Identification id)
    {
        return _inputDataConsumers.Where(x => x.Value.Contains(id)).Select(x => x.Key);
    }


    /// <summary>
    /// Gets the intermediate data consumed by the specified input module.
    /// </summary>
    /// <param name="id">The identification of the input module.</param>
    /// <returns>The consumed intermediate data.</returns>
    public IEnumerable<Identification> GetInputModuleConsumedIntermediateDataIds(Identification id)
    {
        return _intermediateDataInputModuleConsumers.Where(x => x.Value.Contains(id)).Select(x => x.Key);
    }

    /// <summary>
    /// Gets the intermediate data provided by the specified input module.
    /// </summary>
    /// <param name="id">The identification of the input module.</param>
    /// <returns>The provided intermediate data.</returns>
    public IEnumerable<Identification> GetInputModuleProvidedIntermediateDataIds(Identification id)
    {
        return _intermediateDataProviders.Where(x => x.Value == id).Select(x => x.Key);
    }

    /// <summary>
    /// Gets the color attachment of the specified render module.
    /// </summary>
    /// <param name="id">The identification of the render module.</param>
    /// <returns>The color attachment.</returns>
    public List<OneOf<Identification, Swapchain>>? GetRenderModuleColorAttachment(Identification id)
    {
        return _renderModuleColorAttachments.TryGetValue(id, out var value) ? value : null;
    }

    /// <summary>
    /// Gets the depth stencil attachment of the specified render module.
    /// </summary>
    /// <param name="id">The identification of the render module.</param>
    /// <returns>The depth stencil attachment.</returns>
    public Identification? GetRenderModuleDepthStencilAttachment(Identification id)
    {
        return _renderModuleDepthStencilAttachments.TryGetValue(id, out var value) ? value : null;
    }

    /// <summary>
    /// Gets the sampled textures accessed by the specified render module.
    /// </summary>
    /// <param name="id">The identification of the render module.</param>
    /// <returns>The accessed sampled textures.</returns>
    public IEnumerable<Identification> GetRenderModuleSampledTexturesAccessed(Identification id)
    {
        return _renderModuleSampledTexturesAccessed.TryGetValue(id, out var value)
            ? value
            : Enumerable.Empty<Identification>();
    }

    /// <summary>
    /// Gets the storage textures accessed by the specified render module.
    /// </summary>
    /// <param name="id">The identification of the render module.</param>
    /// <returns>The accessed storage textures.</returns>
    public IEnumerable<Identification> GetRenderModuleStorageTexturesAccessed(Identification id)
    {
        return _renderModuleStorageTexturesAccessed.TryGetValue(id, out var value)
            ? value
            : Enumerable.Empty<Identification>();
    }

    #region IInputModuleDataAccessor

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
    public Func<TIntermediateData?> UseIntermediateData<TIntermediateData>(Identification intermediateDataId,
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

    private TIntermediateData? GetIntermediateDataInputModule<TIntermediateData>(Identification intermediateId)
        where TIntermediateData : IntermediateData
    {
        if (!_inputIntermediateData.TryGetValue(intermediateId, out var intermediateData))
        {
            intermediateData = intermediateDataManager.GetCurrentData(intermediateId);

            if (intermediateData is null) return null;
        }

        if (intermediateData is not TIntermediateData castedData)
            throw new MintyCoreException(
                $"Intermediate data {intermediateId} is not of type {typeof(TIntermediateData)}.");

        return castedData;
    }

    /// <inheritdoc />
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

        //Track the used intermediate data for the current frame
        data.IncreaseRefCount();
        _renderModuleIntermediateData[_currentSwapchainImageIndex].Add(data);

        if (data is not TIntermediateData castedData)
            throw new MintyCoreException(
                $"Intermediate data {intermediateId} is not of type {typeof(TIntermediateData)}.");

        return castedData;
    }

    /// <inheritdoc />
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

        _inputIntermediateData.Add(intermediateId, intermediateData);

        return (TIntermediateData)intermediateData;
    }

    /// <summary>
    ///  Validates that all intermediate data is provided and consumed.
    /// </summary>
    /// <exception cref="MintyCoreException"> If intermediate data is consumed but not provided.</exception>
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

    /// <inheritdoc />
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

        if (!_renderModuleColorAttachments.TryGetValue(renderModule.Identification, out var colorAttachments))
        {
            colorAttachments = [];
            _renderModuleColorAttachments.Add(renderModule.Identification, colorAttachments);
        }

        colorAttachments.Add(targetTexture);
    }

    /// <inheritdoc />
    public void SetDepthStencilAttachment(Identification targetDepthTexture, RenderModule renderModule)
    {
        var textureDescription = renderDataManager.GetRenderTextureDescription(targetDepthTexture);

        if ((textureDescription.Usage & TextureUsage.DepthStencil) == 0)
            throw new MintyCoreException("Target texture is not a depth stencil texture");


        _renderModuleDepthStencilAttachments[renderModule.Identification] = targetDepthTexture;
    }

    /// <inheritdoc />
    public Func<DescriptorSet> UseSampledTexture(Identification textureId, RenderModule renderModule, ColorAttachmentSampleMode sampleMode)
    {
        var textureDescription = renderDataManager.GetRenderTextureDescription(textureId);

        if ((textureDescription.Usage & TextureUsage.DepthStencil) != 0)
            throw new MintyCoreException("Depth Texture is not allowed for sampled textures");

        if (_renderModuleStorageTexturesAccessed.TryGetValue(renderModule.Identification,
                out var accessedStorageTextures) && accessedStorageTextures.Contains(textureId))
            throw new MintyCoreException(
                "Texture cannot be used as a sampled texture and a storage texture at the same time");

        if (_renderModuleColorAttachments.TryGetValue(renderModule.Identification, out var colorAttachment) &&
            colorAttachment.Any(x => x.TryPickT0(out var colorAttachmentId, out _) && colorAttachmentId == textureId))
            throw new MintyCoreException(
                "Texture cannot be used as a sampled texture and a color attachment at the same time");

        if (!_renderModuleSampledTexturesAccessed.TryGetValue(renderModule.Identification, out var value))
        {
            value = new List<Identification>();
            _renderModuleSampledTexturesAccessed.Add(renderModule.Identification, value);
        }

        value.Add(textureId);

        return () => renderDataManager.GetSampledTextureDescriptorSet(textureId, sampleMode);
    }

    /// <inheritdoc />
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
            colorAttachment.Any(x => x.TryPickT0(out var colorAttachmentId, out _) && colorAttachmentId == textureId))
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