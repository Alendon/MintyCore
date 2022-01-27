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
    /// <summary />
    public delegate void RegisterDelegate();

    /// <inheritdoc />
    public void PreRegister()
    {
    }

    /// <inheritdoc />
    public void Register()
    {
        Logger.WriteLog("Registering Materials", LogImportance.INFO, "Registry");
        OnRegister.Invoke();
    }

    /// <inheritdoc />
    public void PostRegister()
    {
    }

    /// <inheritdoc />
    public void Clear()
    {
        Logger.WriteLog("Clearing Materials", LogImportance.INFO, "Registry");
        OnRegister = delegate { };
        MaterialHandler.Clear();
    }
        
    public void ClearRegistryEvents()
    {
        OnRegister = delegate { };
    }

    /// <inheritdoc />
    public ushort RegistryId => RegistryIDs.Material;

    /// <inheritdoc />
    public IEnumerable<ushort> RequiredRegistries => new[]
    {
        RegistryIDs.Pipeline,
        /*RegistryIDs.Texture*/
    };

    /// <summary />
    public static event RegisterDelegate OnRegister = delegate { };

    /// <summary>
    ///     Register a <see cref="Material" />
    /// </summary>
    /// <param name="modId"><see cref="ushort" /> id of the mod registering the <see cref="Material" /></param>
    /// <param name="stringIdentifier"><see cref="string" /> id of the <see cref="Material" /></param>
    /// <param name="pipelineId">The <see cref="Pipeline" /> used in the <see cref="Material" /></param>
    /// <param name="descriptorSets">The <see cref="DescriptorSet" /> used in the <see cref="Material" /></param>
    /// <returns>Generated <see cref="Identification" /> for <see cref="Material" /></returns>
    public static Identification RegisterMaterial(ushort modId, string stringIdentifier, Identification pipelineId,
        params (DescriptorSet, uint)[] descriptorSets)
    {
        var materialId = RegistryManager.RegisterObjectId(modId, RegistryIDs.Material, stringIdentifier);
        MaterialHandler.AddMaterial(materialId, pipelineId, descriptorSets);
        return materialId;
    }
}