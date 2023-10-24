using System;
using System.Runtime.InteropServices;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Render.VulkanObjects;

/// <summary>
///     Class to wrap native vulkan shader
/// </summary>
public sealed unsafe class Shader : VulkanObject
{
    public ShaderModule Module { get; }
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

    protected override void ReleaseUnmanagedResources()
    {
        VulkanEngine.Vk.DestroyShaderModule(VulkanEngine.Device, Module,
            null);
        Marshal.FreeHGlobal((IntPtr)_entryPoint);
        _entryPoint = null;
    }


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