using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using MintyCore.Graphics;
using MintyCore.Graphics.Managers;
using MintyCore.Graphics.Render;
using MintyCore.Graphics.Render.Data;
using MintyCore.Graphics.VulkanObjects;
using MintyCore.Identifications;
using MintyCore.Registries;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.UI;

[RegisterInputDataModule("ui")]
public class UiInputModule(
    IVulkanEngine vulkanEngine,
    IMemoryManager memoryManager,
    IDescriptorSetManager descriptorSetManager) : InputModule
{
    private SingletonInputData<UiRenderInputData> _inputData = null!;
    private Func<UiIntermediateData> _intermediateData = null!;
    private MemoryBuffer? _stagingBuffer;


    public override void Setup()
    {
        _inputData = ModuleDataAccessor.UseSingletonInputData<UiRenderInputData>(RenderInputDataIDs.Ui, this);
        _intermediateData =
            ModuleDataAccessor.ProvideIntermediateData<UiIntermediateData>(IntermediateRenderDataIDs.Ui, this);
    }

    public override unsafe void Update(ManagedCommandBuffer commandBuffer)
    {
        var inputData = _inputData.AquireData();
        var intermediateData = _intermediateData();

        intermediateData.InputData = inputData;

        var swapchainExtent = vulkanEngine.SwapchainExtent;

        if (intermediateData.BufferExtent.Height == swapchainExtent.Height &&
            intermediateData.BufferExtent.Width == swapchainExtent.Width)
            return;

        var transformBuffer = intermediateData.TransformBuffer;

        Span<uint> queue = stackalloc uint[] { vulkanEngine.QueueFamilyIndexes.GraphicsFamily!.Value };

        _stagingBuffer ??= memoryManager.CreateBuffer(BufferUsageFlags.TransferSrcBit,
            (ulong)Unsafe.SizeOf<Matrix4x4>(), queue,
            MemoryPropertyFlags.HostCoherentBit | MemoryPropertyFlags.HostVisibleBit, true);

        transformBuffer ??= memoryManager.CreateBuffer(
            BufferUsageFlags.TransferDstBit | BufferUsageFlags.UniformBufferBit,
            (ulong)Unsafe.SizeOf<Matrix4x4>(), queue,
            MemoryPropertyFlags.DeviceLocalBit, false);

        var transform = Matrix4x4.CreateOrthographicOffCenter(0, swapchainExtent.Width,
            0, swapchainExtent.Height, 0, -1);

        _stagingBuffer.MapAs<Matrix4x4>()[0] = transform;

        commandBuffer.CopyBuffer(_stagingBuffer, transformBuffer);

        var transformDescriptorSet = descriptorSetManager.AllocateDescriptorSet(DescriptorSetIDs.UiTransformBuffer);

        DescriptorBufferInfo bufferInfo = new()
        {
            Buffer = transformBuffer.Buffer,
            Offset = 0,
            Range = transformBuffer.Size
        };

        WriteDescriptorSet write = new()
        {
            SType = StructureType.WriteDescriptorSet,
            DescriptorCount = 1,
            DescriptorType = DescriptorType.UniformBuffer,
            DstBinding = 0,
            DstSet = transformDescriptorSet,
            DstArrayElement = 0,
            PBufferInfo = &bufferInfo
        };

        vulkanEngine.Vk.UpdateDescriptorSets(vulkanEngine.Device, 1, write, 0, null);

        intermediateData.TransformBuffer = transformBuffer;
        intermediateData.TransformDescriptorSet = transformDescriptorSet;
        intermediateData.BufferExtent = swapchainExtent;
    }

    public override Identification Identification => RenderInputModuleIDs.Ui;

    public override void Dispose()
    {
        _stagingBuffer?.Dispose();
    }
}