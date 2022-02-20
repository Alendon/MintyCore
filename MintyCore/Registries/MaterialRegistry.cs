using System;
using System.Collections.Generic;
using MintyCore.Identifications;
using MintyCore.Render;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Registries;

/// <summary>
///     The <see cref="IRegistry" /> class for all <see cref="Material" />
/// </summary>
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
    public void ClearRegistryEvents()
    {
        OnRegister = delegate { };
        OnPostRegister = delegate { };
        OnPreRegister = delegate { };
    }


    /// <inheritdoc />
    public void Clear()
    {
        Logger.WriteLog("Clearing Materials", LogImportance.INFO, "Registry");
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
    public static Identification RegisterMaterial(ushort modId, string stringIdentifier, Identification pipelineId,
        params (DescriptorSet, uint)[] descriptorSets)
    {
        RegistryManager.AssertMainObjectRegistryPhase();
        var materialId = RegistryManager.RegisterObjectId(modId, RegistryIDs.Material, stringIdentifier);
        MaterialHandler.AddMaterial(materialId, pipelineId, descriptorSets);
        return materialId;
    }
}