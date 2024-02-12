using System;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Graphics.VulkanObjects;

/// <summary>
///     Class to wrap native vulkan shader
/// </summary>
[PublicAPI]
public sealed unsafe class Shader : VulkanObject
{
    /// <summary>
    ///   The internal shader module
    /// </summary>
    public ShaderModule Module { get; }
    
    /// <summary>
    ///  The stage flags of the shader
    /// </summary>
    public ShaderStageFlags StageFlags { get; }
    private bool _disposed;
    private byte* _entryPoint;

    internal Shader(IVulkanEngine vulkanEngine, IAllocationHandler allocationHandler, ShaderModule shaderModule,
        string entryPoint, ShaderStageFlags stageFlags) : base(vulkanEngine, allocationHandler)
    {
        Module = shaderModule;
        _disposed = false;
        _entryPoint = (byte*)Marshal.StringToHGlobalAnsi(entryPoint);
        StageFlags = stageFlags;
    }

    /// <inheritdoc />
    protected override void ReleaseUnmanagedResources()
    {
        VulkanEngine.Vk.DestroyShaderModule(VulkanEngine.Device, Module,
            null);
        Marshal.FreeHGlobal((IntPtr)_entryPoint);
        _entryPoint = null;
    }


    /// <summary>
    ///  Gets the pipeline shader stage create info
    /// </summary>
    public PipelineShaderStageCreateInfo GetPipelineShaderStageCreateInfo()
    {
        return new PipelineShaderStageCreateInfo
        {
            SType = StructureType.PipelineShaderStageCreateInfo,
            PNext = null,
            Flags = 0,
            Module = Module,
            Stage = StageFlags,
            PName = _entryPoint
        };
    }
}