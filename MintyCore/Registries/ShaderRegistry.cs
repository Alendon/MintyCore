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
///     The <see cref="IRegistry" /> class for all <see cref="Shader" />
/// </summary>
[Registry("shader")]
public class ShaderRegistry : IRegistry
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
        ShaderHandler.RemoveShader(objectId);
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
        Logger.WriteLog("Clearing Shaders", LogImportance.INFO, "Registry");
        ClearRegistryEvents();
        ShaderHandler.Clear();
    }


    /// <inheritdoc />
    public ushort RegistryId => RegistryIDs.Shader;

    /// <inheritdoc />
    public IEnumerable<ushort> RequiredRegistries => Array.Empty<ushort>();

    /// <summary />
    public static event Action OnRegister = delegate { };

    /// <summary />
    public static event Action OnPostRegister = delegate { };

    /// <summary />
    public static event Action OnPreRegister = delegate { };

    [RegisterMethod(ObjectRegistryPhase.MAIN, true)]
    public static void RegisterShader(Identification shaderId,
        ShaderInfo shaderInfo)
    {
        if (Engine.HeadlessModeActive) return;
        ShaderHandler.AddShader(shaderId, shaderInfo.Stage, shaderInfo.EntryPoint);
    }


    /// <summary>
    ///     Register a <see cref="Shader" />
    ///     Call this at <see cref="OnRegister" />
    /// </summary>
    /// <param name="modId"><see cref="ushort" /> id of the mod registering the <see cref="Shader" /></param>
    /// <param name="stringIdentifier"><see cref="string" /> id of the <see cref="Shader" /></param>
    /// <param name="shaderName">The file name of the <see cref="Shader" /></param>
    /// <param name="shaderStage">The <see cref="ShaderStageFlags" /> of the <see cref="Shader" /></param>
    /// <param name="shaderEntryPoint">The entry point (main method) of the <see cref="Shader" /></param>
    /// <returns>Generated <see cref="Identification" /> for <see cref="Shader" /></returns>
    public static Identification RegisterShader(ushort modId, string stringIdentifier, string shaderName,
        ShaderStageFlags shaderStage, string shaderEntryPoint = "main")
    {
        RegistryManager.AssertMainObjectRegistryPhase();
        var shaderId =
            RegistryManager.RegisterObjectId(modId, RegistryIDs.Shader, stringIdentifier, shaderName);
        if (Engine.HeadlessModeActive)
            return shaderId;
        ShaderHandler.AddShader(shaderId, shaderStage, shaderEntryPoint);
        return shaderId;
    }
}

public struct ShaderInfo
{
    public ShaderInfo(ShaderStageFlags flags, string entryPoint = "main")
    {
        Stage = flags;
        EntryPoint = entryPoint;
    }
    
    public ShaderStageFlags Stage;
    public string EntryPoint;
}