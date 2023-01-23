using System;
using System.Collections.Generic;
using System.Numerics;
using MintyCore.Components.Client;
using MintyCore.Components.Common;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Registries;
using MintyCore.Render;
using MintyCore.SystemGroups;
using MintyCore.Utils;
using Silk.NET.Vulkan;
using Buffer = System.Buffer;

namespace MintyCore.Systems.Client;

/// <summary>
///     System to instanced render entities
/// </summary>
[RegisterSystem("render_instanced")]
[ExecuteInSystemGroup<PresentationSystemGroup>]
[ExecuteAfter<ApplyGpuCameraBufferSystem>]
[ExecutionSide(GameType.Client)]
public sealed unsafe partial class RenderInstancedSystem : ARenderSystem
{
    private const int InitialSize = 512;

    [ComponentQuery] private readonly CameraComponentQuery<object, Camera> _cameraComponentQuery = new();
    [ComponentQuery] private readonly ComponentQuery<object, (InstancedRenderAble, Transform)> _componentQuery = new();
    private readonly Dictionary<Identification, uint> _drawCount = new();

    private readonly Dictionary<Identification, MemoryBuffer[]> _instanceBuffers = new();

    private readonly Dictionary<Identification, (MemoryBuffer buffer, IntPtr mappedData, int capacity, int currentIndex
            )>
        _stagingBuffers = new();

    /// <inheritdoc />
    public override Identification Identification => SystemIDs.RenderInstanced;

    /// <inheritdoc />
    protected override void Execute()
    {
        //Iterate over each entity and write the current transform to the corresponding staging buffer
        _drawCount.Clear();
        foreach (var entity in _componentQuery)
        {
            var renderAble = entity.GetInstancedRenderAble();
            var transform = entity.GetTransform();

            if (renderAble.MaterialMeshCombination == Identification.Invalid) continue;

            WriteToBuffer(renderAble.MaterialMeshCombination, transform.Value);

            if (!_drawCount.ContainsKey(renderAble.MaterialMeshCombination))
                _drawCount.Add(renderAble.MaterialMeshCombination, 0);
            _drawCount[renderAble.MaterialMeshCombination] += 1;
        }

        //submit the staging buffers and write to each instance buffer
        SubmitBuffers();


        foreach (var cameraEntity in _cameraComponentQuery)
        {
            var camera = cameraEntity.GetCamera();
            foreach (var (id, drawCount) in _drawCount)
            {
                var (mesh, material) = InstancedRenderDataHandler.GetMeshMaterial(id);
                var instanceBuffer = _instanceBuffers[id][VulkanEngine.ImageIndex];

                for (var i = 0; i < mesh.SubMeshIndexes.Length; i++)
                {
                    var (startIndex, length) = mesh.SubMeshIndexes[i];

                    material[i].Bind(CommandBuffer);

                    if (camera.GpuTransformDescriptors.Length == 0) break;

                    VulkanEngine.Vk.CmdBindDescriptorSets(CommandBuffer, PipelineBindPoint.Graphics,
                        material[i].PipelineLayout,
                        0, camera.GpuTransformDescriptors.AsSpan().Slice((int) VulkanEngine.ImageIndex, 1), 0,
                        null);

                    VulkanEngine.Vk.CmdBindVertexBuffers(CommandBuffer, 0, 1, mesh.MemoryBuffer.Buffer, 0);
                    VulkanEngine.Vk.CmdBindVertexBuffers(CommandBuffer, 1, 1, instanceBuffer.Buffer, 0);


                    VulkanEngine.Vk.CmdDraw(CommandBuffer, length, drawCount, startIndex, 0);
                }
            }
        }
    }

    private void SubmitBuffers()
    {
        Span<uint> queueFamilies = stackalloc uint[] {VulkanEngine.QueueFamilyIndexes.PresentFamily!.Value};

        var commandBuffer = VulkanEngine.GetSingleTimeCommandBuffer();

        foreach (var (id, (buffer, _, capacity, index)) in _stagingBuffers)
        {
            MemoryManager.UnMap(buffer.Memory);

            MemoryBuffer instanceBuffer;
            if (!_instanceBuffers.ContainsKey(id))
            {
                _instanceBuffers.Add(id, new MemoryBuffer[VulkanEngine.SwapchainImageCount]);
                for (var i = 0; i < VulkanEngine.SwapchainImageCount; i++)
                {
                    instanceBuffer = MemoryBuffer.Create(
                        BufferUsageFlags.TransferDstBit | BufferUsageFlags.VertexBufferBit,
                        buffer.Size, SharingMode.Exclusive, queueFamilies,
                        MemoryPropertyFlags.DeviceLocalBit, false);
                    _instanceBuffers[id][i] = instanceBuffer;
                }
            }

            instanceBuffer = _instanceBuffers[id][VulkanEngine.ImageIndex];
            if (instanceBuffer.Size < buffer.Size)
            {
                instanceBuffer.Dispose();

                instanceBuffer = MemoryBuffer.Create(
                    BufferUsageFlags.TransferDstBit | BufferUsageFlags.VertexBufferBit,
                    buffer.Size, SharingMode.Exclusive, queueFamilies,
                    MemoryPropertyFlags.DeviceLocalBit, false);
                _instanceBuffers[id][VulkanEngine.ImageIndex] = instanceBuffer;
            }

            _stagingBuffers[id] = (buffer, IntPtr.Zero, capacity, 0);

            BufferCopy bufferCopy = new()
            {
                Size = (ulong) (index * sizeof(Matrix4x4)),
                DstOffset = 0,
                SrcOffset = 0
            };
            VulkanEngine.Vk.CmdCopyBuffer(commandBuffer, buffer.Buffer, instanceBuffer.Buffer, 1, bufferCopy);
        }

        VulkanEngine.ExecuteSingleTimeCommandBuffer(commandBuffer);
    }

    private void WriteToBuffer(Identification materialMesh, in Matrix4x4 transformData)
    {
        Span<uint> queueFamilies = stackalloc uint[] {VulkanEngine.QueueFamilyIndexes.PresentFamily!.Value};
        if (!_stagingBuffers.ContainsKey(materialMesh))
        {
            var memoryBuffer = MemoryBuffer.Create(BufferUsageFlags.TransferSrcBit,
                (ulong) (sizeof(Matrix4x4) * InitialSize), SharingMode.Exclusive, queueFamilies,
                MemoryPropertyFlags.HostVisibleBit |
                MemoryPropertyFlags.HostCoherentBit, true);

            _stagingBuffers.Add(materialMesh, (memoryBuffer, MemoryManager.Map(memoryBuffer.Memory), InitialSize, 0));
        }

        var (buffer, data, capacity, index) = _stagingBuffers[materialMesh];

        if (data == IntPtr.Zero) data = MemoryManager.Map(buffer.Memory);

        if (capacity <= index)
        {
            var memoryBuffer = MemoryBuffer.Create(BufferUsageFlags.TransferSrcBit,
                (ulong) (sizeof(Matrix4x4) * capacity * 2), SharingMode.Exclusive, queueFamilies,
                MemoryPropertyFlags.HostVisibleBit |
                MemoryPropertyFlags.HostCoherentBit, true);

            var oldData = (Transform*) data;
            var newData = (Transform*) MemoryManager.Map(memoryBuffer.Memory);

            Buffer.MemoryCopy(oldData, newData, sizeof(Matrix4x4) * capacity * 2, sizeof(Matrix4x4) * capacity);

            MemoryManager.UnMap(buffer.Memory);
            buffer.Dispose();

            buffer = memoryBuffer;
            data = (IntPtr) newData;
            capacity *= 2;
        }

        // ReSharper disable once PossibleNullReferenceException
        ((Matrix4x4*) data)[index] = transformData;

        _stagingBuffers[materialMesh] = (buffer, data, capacity, index + 1);
    }

    /// <inheritdoc />
    public override void Setup(SystemManager systemManager)
    {
        _componentQuery.Setup(this);
        _cameraComponentQuery.Setup(this);

        SetRenderArguments(new RenderPassArguments
        {
            RenderPass = RenderPassIDs.Main
        });
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        foreach (var (_, memoryBuffers) in _instanceBuffers)
        foreach (var memoryBuffer in memoryBuffers)
            memoryBuffer.Dispose();

        foreach (var (_, stagingBuffer) in _stagingBuffers) stagingBuffer.buffer.Dispose();

        base.Dispose();
    }
}