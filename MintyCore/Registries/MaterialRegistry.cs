using System;
using System.Collections.Generic;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Modding.Attributes;
using MintyCore.Render;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Registries;

/// <summary>
///     The <see cref="IRegistry" /> class for all <see cref="Material" />
/// </summary>
[Registry("material")]
public class MaterialRegistry : IRegistry
{
    /// <inheritdoc />
    public void PreRegister()
    {
        OnPreRegister();
    }

    /// <inheritdoc />
    public void Register()
    {
        OnRegister();
    }

    /// <inheritdoc />
    public void PostRegister()
    {
        OnPostRegister();
    }

    /// <inheritdoc />
    public void PreUnRegister()
    {
    }

    /// <inheritdoc />
    public void UnRegister(Identification objectId)
    {
        if (Engine.HeadlessModeActive)
            return;
        MaterialHandler.RemoveMaterial(objectId);
    }

    /// <inheritdoc />
    public void PostUnRegister()
    {
    }

    /// <inheritdoc />
    public void ClearRegistryEvents()
    {
        OnRegister = delegate { };
        OnPostRegister = delegate { };
        OnPreRegister = delegate { };
    }


    /// <inheritdoc />
    public void Clear()
    {
        Logger.WriteLog("Clearing Materials", LogImportance.Info, "Registry");
        ClearRegistryEvents();
        MaterialHandler.Clear();
    }


    /// <inheritdoc />
    public ushort RegistryId => RegistryIDs.Material;

    /// <inheritdoc />
    public IEnumerable<ushort> RequiredRegistries => new[]
    {
        RegistryIDs.Pipeline
        /*RegistryIDs.Texture*/
    };

    /// <summary />
    public static event Action OnRegister = delegate { };

    /// <summary />
    public static event Action OnPostRegister = delegate { };

    /// <summary />
    public static event Action OnPreRegister = delegate { };


    /// <summary>
    ///     Register a <see cref="Material" />
    ///     Call this at <see cref="OnRegister" />
    /// </summary>
    /// <param name="modId"><see cref="ushort" /> id of the mod registering the <see cref="Material" /></param>
    /// <param name="stringIdentifier"><see cref="string" /> id of the <see cref="Material" /></param>
    /// <param name="pipelineId">The <see cref="Pipeline" /> used in the <see cref="Material" /></param>
    /// <param name="descriptorSets">The <see cref="DescriptorSet" /> used in the <see cref="Material" /></param>
    /// <returns>Generated <see cref="Identification" /> for <see cref="Material" /></returns>
    [Obsolete]
    public static Identification RegisterMaterial(ushort modId, string stringIdentifier, Identification pipelineId,
        params (Identification, uint)[] descriptorSets)
    {
        RegistryManager.AssertMainObjectRegistryPhase();
        var materialId = RegistryManager.RegisterObjectId(modId, RegistryIDs.Material, stringIdentifier);
        if(Engine.HeadlessModeActive)
            return materialId;
        MaterialHandler.AddMaterial(materialId, pipelineId, descriptorSets);
        return materialId;
    }

    /// <summary>
    /// Register a <see cref="Material" />
    /// Used by the source generator for <see cref="Registries.RegisterMaterialAttribute"/>
    /// </summary>
    /// <param name="id">Id of the material</param>
    /// <param name="info">Info for creating the material</param>
    [RegisterMethod(ObjectRegistryPhase.Main)]
    public static void RegisterMaterial(Identification id, MaterialInfo info)
    {
        if(Engine.HeadlessModeActive)
            return;
        
        MaterialHandler.AddMaterial(id, info.PipelineId, info.DescriptorSets);
    }

    /// <summary>
    /// Register a descriptor set handler
    /// Call this at <see cref="OnRegister" />
    /// </summary>
    /// <remarks>
    /// The handler is a function that returns a <see cref="DescriptorSet" /> for a given <see cref="Identification"/>
    /// for example the texture descriptor set handler returns the sampled image descriptor set for a given texture
    /// </remarks>
    /// <param name="modId"></param>
    /// <param name="stringIdentifier"></param>
    /// <param name="categoryId"></param>
    /// <param name="descriptorFetchFunc"></param>
    /// <returns></returns>
    [Obsolete]
    public static Identification RegisterDescriptorHandler(ushort modId, string stringIdentifier, ushort categoryId,
        Func<Identification, DescriptorSet> descriptorFetchFunc)
    {
        RegistryManager.AssertPreObjectRegistryPhase();
        var id = RegistryManager.RegisterObjectId(modId, RegistryIDs.Material, stringIdentifier);
        if(Engine.HeadlessModeActive)
            return id;
        MaterialHandler.AddDescriptorHandler(id, categoryId, descriptorFetchFunc);

        return id;
    }

    /// <summary>
    /// Register a descriptor set handler
    /// Used by the source generator for <see cref="Registries.RegisterDescriptorHandlerAttribute"/>
    /// </summary>
    /// <param name="id">Id of the handler</param>
    /// <param name="info">Info for creating the handler</param>
    [RegisterMethod(ObjectRegistryPhase.Pre)]
    public static void RegisterDescriptorHandler(Identification id, DescriptorHandlerInfo info)
    {
        if(Engine.HeadlessModeActive)
            return;
        MaterialHandler.AddDescriptorHandler(id, info.CategoryId, info.DescriptorFetchFunc);
    }
}

/// <summary>
///  Info for creating a <see cref="Material" />
/// </summary>
public struct MaterialInfo
{
    /// <summary>
    /// Id of the pipeline to use in the material
    /// </summary>
    public Identification PipelineId;
    
    /// <summary>
    /// Descriptor sets to use in the material
    /// </summary>
    public (Identification, uint)[] DescriptorSets;
}

/// <summary>
/// Info for adding a descriptor set handler
/// </summary>
public struct DescriptorHandlerInfo
{
    /// <summary>
    /// Category to use for the handler
    /// </summary>
    public ushort CategoryId;
    
    /// <summary>
    /// Method to fetch the descriptor set
    /// </summary>
    public Func<Identification, DescriptorSet> DescriptorFetchFunc;
}