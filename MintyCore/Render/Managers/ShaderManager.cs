using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MintyCore.Modding;
using MintyCore.Registries;
using MintyCore.Render.Managers.Interfaces;
using MintyCore.Render.Utils;
using MintyCore.Render.VulkanObjects;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Render.Managers;

/// <summary>
///     The handler for all shader specific stuff. Get populated by the <see cref="ShaderRegistry" />
/// </summary>
[Singleton<IShaderManager>(SingletonContextFlags.NoHeadless)]
internal class ShaderManager : IShaderManager
{
    private readonly Dictionary<Identification, Shader> _shaders = new();

    public required IModManager ModManager { init; private get; }
    public required IVulkanEngine VulkanEngine { init; private get; }
    public required IAllocationHandler AllocationHandler { init; private get; }
    
    public void AddShader(Identification shaderId, ShaderStageFlags shaderStage, string shaderEntryPoint)
    {
        var shaderFileStream = ModManager.GetResourceFileStream(shaderId);
        var shaderCode = new byte[shaderFileStream.Length];
        Logger.AssertAndThrow(shaderFileStream.Read(shaderCode, 0, shaderCode.Length) == shaderCode.Length,
            "Failed to fully read shader code from file stream", "ShaderHandler");


        _shaders.Add(shaderId, CreateShader(shaderCode, shaderEntryPoint, shaderStage));
    }

    public unsafe void AddShader(Identification shaderId, ShaderStageFlags shaderStage, string shaderEntryPoint, ReadOnlySpan<uint> shaderCode)
    {
        ShaderModule module;
        fixed (uint* shaderPtr = &shaderCode[0])
        {
            ShaderModuleCreateInfo shaderCreateInfo = new()
            {
                SType = StructureType.ShaderModuleCreateInfo,
                CodeSize = (nuint) shaderCode.Length * sizeof(uint),
                PCode = shaderPtr
            };

            VulkanUtils.Assert(VulkanEngine.Vk.CreateShaderModule(VulkanEngine.Device, shaderCreateInfo,
                null,
                out module));
        }
        
        _shaders.Add(shaderId, new Shader(VulkanEngine, AllocationHandler, module, shaderEntryPoint, shaderStage));
    }
    
    internal unsafe Shader CreateShader(byte[] shaderCode, string entryPoint, ShaderStageFlags stageFlags)
    {
        ShaderModule module;
        fixed (byte* shaderPtr = &shaderCode[0])
        {
            ShaderModuleCreateInfo shaderCreateInfo = new()
            {
                SType = StructureType.ShaderModuleCreateInfo,
                CodeSize = (nuint) shaderCode.Length,
                PCode = (uint*) shaderPtr
            };

            VulkanUtils.Assert(VulkanEngine.Vk.CreateShaderModule(VulkanEngine.Device, shaderCreateInfo,
                null,
                out module));
        }

        return new Shader(VulkanEngine, AllocationHandler, module, entryPoint, stageFlags);
    }

    /// <summary>
    ///     Get a <see cref="Shader" />
    /// </summary>
    /// <param name="shaderId"><see cref="Identification" /> of the <see cref="Shader" /></param>
    /// <returns></returns>
    public Shader GetShader(Identification shaderId)
    {
        return _shaders[shaderId];
    }

    public void Clear()
    {
        foreach (var shader in _shaders) shader.Value.Dispose();
        _shaders.Clear();
    }

    public void RemoveShader(Identification objectId)
    {
        if (_shaders.Remove(objectId, out var shader)) shader.Dispose();
    }
}