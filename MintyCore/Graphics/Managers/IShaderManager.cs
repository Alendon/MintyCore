using System;
using MintyCore.Graphics.VulkanObjects;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Graphics.Managers;

/// <summary>
/// Manages the creation and deletion of shaders
/// </summary>
public interface IShaderManager
{
    /// <summary>
    /// Adds a shader to the manager with the given ID, stage, and entry point.
    /// </summary>
    /// <param name="shaderId">The unique identifier for the shader.</param>
    /// <param name="shaderStage">The stage of the shader pipeline that this shader is part of.</param>
    /// <param name="shaderEntryPoint">The entry point function where the shader starts executing.</param>
    void AddShader(Identification shaderId, ShaderStageFlags shaderStage, string shaderEntryPoint);

    /// <summary>
    /// Adds a shader to the manager with the given ID, stage, entry point, and shader code.
    /// </summary>
    /// <param name="shaderId">The unique identifier for the shader.</param>
    /// <param name="shaderStage">The stage of the shader pipeline that this shader is part of.</param>
    /// <param name="shaderEntryPoint">The entry point function where the shader starts executing.</param>
    /// <param name="shaderCode">The code of the shader in a read-only span of uints.</param>
    void AddShader(Identification shaderId, ShaderStageFlags shaderStage, string shaderEntryPoint,
        ReadOnlySpan<uint> shaderCode);

    /// <summary>
    /// Retrieves a shader from the manager by its ID.
    /// </summary>
    /// <param name="shaderId">The unique identifier of the shader to retrieve.</param>
    /// <returns>The shader with the given ID.</returns>
    Shader GetShader(Identification shaderId);

    /// <summary>
    ///  Clear all internal data
    /// </summary>
    void Clear();
    
    /// <summary>
    ///  Remove a shader from the manager
    /// </summary>
    void RemoveShader(Identification objectId);
}