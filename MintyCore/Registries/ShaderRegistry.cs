using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using JetBrains.Annotations;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Modding.Attributes;
using MintyCore.Modding.Implementations;
using MintyCore.Render.Managers.Interfaces;
using MintyCore.Render.VulkanObjects;
using MintyCore.Utils;
using Serilog;
using Silk.NET.Vulkan;

namespace MintyCore.Registries;

/// <summary>
///     The <see cref="IRegistry" /> class for all <see cref="Shader" />
/// </summary>
[Registry("shader", "shaders", applicableGameType: GameType.Client)]
[PublicAPI]
public class ShaderRegistry : IRegistry
{
    public required IShaderManager ShaderManager { private get; init; }

    /// <inheritdoc />
    public void UnRegister(Identification objectId)
    {
        if (Engine.HeadlessModeActive)
            return;
        ShaderManager.RemoveShader(objectId);
    }

    /// <inheritdoc />
    public void Clear()
    {
        Log.Information("Clearing Shaders");
        ShaderManager.Clear();
    }


    /// <inheritdoc />
    public ushort RegistryId => RegistryIDs.Shader;

    /// <inheritdoc />
    public IEnumerable<ushort> RequiredRegistries => Array.Empty<ushort>();

    /// <summary>
    /// Register a new shader
    /// This method is used by the source generator for the auto registry
    /// </summary>
    /// <param name="shaderId"></param>
    /// <param name="shaderInfo"></param>
    [RegisterMethod(ObjectRegistryPhase.Main, RegisterMethodOptions.HasFile)]
    public void RegisterShader(Identification shaderId,
        ShaderInfo shaderInfo)
    {
        if (Engine.HeadlessModeActive) return;
        ShaderManager.AddShader(shaderId, shaderInfo.Stage, shaderInfo.EntryPoint);
    }
    
    [RegisterMethod(ObjectRegistryPhase.Main)]
    public void RegisterShader2(Identification shaderId,
        ShaderInfo2 shaderInfo)
    {
        if (Engine.HeadlessModeActive) return;
        ShaderManager.AddShader(shaderId, shaderInfo.Stage, shaderInfo.EntryPoint, shaderInfo.ShaderCode);
    }
}

/// <summary>
/// Wrapper struct to register a new shader
/// </summary>
public struct ShaderInfo
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="flags">Shader flags</param>
    /// <param name="entryPoint">Main method / entry point method for this shader. One file can be used for multiple shader objects with different entry points</param>
    public ShaderInfo(ShaderStageFlags flags, string entryPoint = "main")
    {
        Stage = flags;
        EntryPoint = entryPoint;
    }

    /// <summary>
    /// Shader stage flags
    /// </summary>
    public readonly ShaderStageFlags Stage;

    /// <summary>
    ///     The entry point method for this shader. One file can be used for multiple shader objects with different entry points
    /// </summary>
    public readonly string EntryPoint;
}

// another shader info struct. But this one directly contains the shader code
public ref struct ShaderInfo2
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="flags">Shader flags</param>
    /// <param name="entryPoint">Main method / entry point method for this shader. One file can be used for multiple shader objects with different entry points</param>
    /// <param name="shaderCode">The shader code</param>
    public ShaderInfo2(ShaderStageFlags flags, ReadOnlySpan<uint> shaderCode, string entryPoint = "main")
    {
        Stage = flags;
        EntryPoint = entryPoint;
        ShaderCode = shaderCode;
    }

    /// <summary>
    /// Shader stage flags
    /// </summary>
    public readonly ShaderStageFlags Stage;

    /// <summary>
    ///     The entry point method for this shader. One file can be used for multiple shader objects with different entry points
    /// </summary>
    public readonly string EntryPoint;

    /// <summary>
    /// The shader code
    /// </summary>
    public readonly ReadOnlySpan<uint> ShaderCode;
}