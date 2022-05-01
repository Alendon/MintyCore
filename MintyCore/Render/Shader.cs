using System;
using System.Runtime.InteropServices;
using Silk.NET.Vulkan;
using static MintyCore.Render.VulkanUtils;

namespace MintyCore.Render;

/// <summary>
///     Class to wrap native vulkan shader
/// </summary>
public unsafe class Shader : IDisposable
{
    private readonly ShaderModule _shaderModule;
    private readonly ShaderStageFlags _stageFlags;
    private bool _disposed;
    private byte* _entryPoint;

    private Shader(ShaderModule shaderModule, string entryPoint, ShaderStageFlags stageFlags)
    {
        _shaderModule = shaderModule;
        _disposed = false;
        _entryPoint = (byte*) Marshal.StringToHGlobalAnsi(entryPoint);
        _stageFlags = stageFlags;
    }


    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        VulkanEngine.Vk.DestroyShaderModule(VulkanEngine.Device, _shaderModule,
            VulkanEngine.AllocationCallback);
        Marshal.FreeHGlobal((IntPtr) _entryPoint);
        _entryPoint = null;
    }

    internal PipelineShaderStageCreateInfo GetCreateInfo()
    {
        return new PipelineShaderStageCreateInfo
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            PNext = null,
            Flags = 0,
            Module = _shaderModule,
            Stage = _stageFlags,
            PName = _entryPoint
        };
    }

    internal static Shader CreateShader(byte[] shaderCode, string entryPoint, ShaderStageFlags stageFlags)
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

            Assert(VulkanEngine.Vk.CreateShaderModule(VulkanEngine.Device, shaderCreateInfo,
                VulkanEngine.AllocationCallback,
                out module));
        }

        return new Shader(module, entryPoint, stageFlags);
    }
}