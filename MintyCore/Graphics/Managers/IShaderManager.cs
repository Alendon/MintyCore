using System;
using MintyCore.Graphics.VulkanObjects;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Graphics.Managers;

public interface IShaderManager
{
    void AddShader(Identification shaderId, ShaderStageFlags shaderStage, string shaderEntryPoint);
    void AddShader(Identification shaderId, ShaderStageFlags shaderStage, string shaderEntryPoint, ReadOnlySpan<uint> shaderCode);

    /// <summary>
    ///     Get a <see cref="Shader" />
    /// </summary>
    /// <param name="shaderId"><see cref="Identification" /> of the <see cref="Shader" /></param>
    /// <returns></returns>
    Shader GetShader(Identification shaderId);

    void Clear();
    void RemoveShader(Identification objectId);
}