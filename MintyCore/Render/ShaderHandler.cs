using System.Collections.Generic;
using MintyCore.Modding;
using MintyCore.Registries;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Render;

/// <summary>
///     The handler for all shader specific stuff. Get populated by the <see cref="ShaderRegistry" />
/// </summary>
public static class ShaderHandler
{
    private static readonly Dictionary<Identification, Shader> _shaders = new();

    internal static void AddShader(Identification shaderId, ShaderStageFlags shaderStage, string shaderEntryPoint)
    {
        var shaderFileStream = ModManager.GetResourceFileStream(shaderId);
        var shaderCode = new byte[shaderFileStream.Length];
        Logger.AssertAndThrow(shaderFileStream.Read(shaderCode, 0, shaderCode.Length) == shaderCode.Length,
            "Failed to fully read shader code from file stream", "ShaderHandler");


        _shaders.Add(shaderId, Shader.CreateShader(shaderCode, shaderEntryPoint, shaderStage));
    }

    /// <summary>
    ///     Get a <see cref="Shader" />
    /// </summary>
    /// <param name="shaderId"><see cref="Identification" /> of the <see cref="Shader" /></param>
    /// <returns></returns>
    public static Shader GetShader(Identification shaderId)
    {
        return _shaders[shaderId];
    }

    internal static void Clear()
    {
        foreach (var shader in _shaders) shader.Value.Dispose();
        _shaders.Clear();
    }

    internal static void RemoveShader(Identification objectId)
    {
        if (_shaders.Remove(objectId, out var shader)) shader.Dispose();
    }
}