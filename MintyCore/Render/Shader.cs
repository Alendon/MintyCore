using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Vulkan;
using static MintyCore.Render.VulkanUtils;

namespace MintyCore.Render
{
    public class Shader : IDisposable
    {
        private readonly ShaderModule _shaderModule;
        private bool _disposed;

        private Shader(ShaderModule shaderModule)
        {
            _shaderModule = shaderModule;
            _disposed = false;
        }

        public unsafe ShaderStageContainer GetShaderStageContainer(ShaderStageFlags stageFlags, string entryPoint,
            SpecializationInfo* specializationInfo = null, PipelineShaderStageCreateFlags shaderStageCreateFlags = 0)
        {
            return new ShaderStageContainer(_shaderModule, entryPoint, stageFlags, shaderStageCreateFlags,
                specializationInfo);
        }

        public ShaderStageContainer GetShaderStageContainer(ShaderStageFlags stageFlags, string entryPoint,
            ref SpecializationInfo specializationInfo, PipelineShaderStageCreateFlags shaderStageCreateFlags = 0)
        {
            return new ShaderStageContainer(_shaderModule, entryPoint, stageFlags, shaderStageCreateFlags,
                ref specializationInfo);
        }

        internal static unsafe Shader CreateShader(byte[] shaderCode)
        {
            ShaderModule module;
            fixed (byte* shaderPtr = &shaderCode[0])
            {
                ShaderModuleCreateInfo shaderCreateInfo = new()
                {
                    SType = StructureType.ShaderModuleCreateInfo,
                    CodeSize = (nuint)shaderCode.Length,
                    PCode = (uint*)shaderPtr
                };

                Assert(VulkanEngine._vk.CreateShaderModule(VulkanEngine._device, shaderCreateInfo,
                    VulkanEngine._allocationCallback,
                    out module));
            }

            return new Shader(module);
        }

        public unsafe void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            VulkanEngine._vk.DestroyShaderModule(VulkanEngine._device, _shaderModule,
                VulkanEngine._allocationCallback);
        }
    }

    public unsafe struct ShaderStageContainer : IDisposable
    {
        internal PipelineShaderStageCreateInfo ShaderStageCreateInfo { get; private set; }

        internal ShaderStageContainer(ShaderModule module, string entryPoint,
            ShaderStageFlags shaderStage, PipelineShaderStageCreateFlags flags, SpecializationInfo* specializationInfo)
        {
            var strPtr = Marshal.StringToHGlobalAnsi(entryPoint);

            ShaderStageCreateInfo = new PipelineShaderStageCreateInfo()
            {
                SType = StructureType.PipelineShaderStageCreateInfo,
                PNext = null,
                Module = module,
                Stage = shaderStage,
                PName = (byte*)strPtr,
                PSpecializationInfo = specializationInfo,
                Flags = flags
            };
        }

        internal ShaderStageContainer(ShaderModule module, string entryPoint,
            ShaderStageFlags shaderStage, PipelineShaderStageCreateFlags flags,
            ref SpecializationInfo specializationInfo)
        {
            var strPtr = Marshal.StringToHGlobalAnsi(entryPoint);

            ShaderStageCreateInfo = new PipelineShaderStageCreateInfo()
            {
                SType = StructureType.PipelineShaderStageCreateInfo,
                PNext = null,
                Module = module,
                Stage = shaderStage,
                PName = (byte*)strPtr,
                PSpecializationInfo = (SpecializationInfo*)Unsafe.AsPointer(ref specializationInfo),
                Flags = flags
            };
        }

        public void Dispose()
        {
            if (ShaderStageCreateInfo.PName == null) return;

            Marshal.FreeHGlobal((IntPtr)ShaderStageCreateInfo.PName);
            var internalCopy = ShaderStageCreateInfo;
            internalCopy.PName = null;
            ShaderStageCreateInfo = internalCopy;
        }
    }
}