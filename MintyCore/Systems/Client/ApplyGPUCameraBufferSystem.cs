using System;
using System.Numerics;
using MintyCore.Components.Client;
using MintyCore.Components.Common;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Registries;
using MintyCore.Render;
using MintyCore.SystemGroups;
using MintyCore.Utils;
using MintyCore.Utils.UnmanagedContainers;
using Silk.NET.Vulkan;

namespace MintyCore.Systems.Client;

[ExecuteInSystemGroup(typeof(PresentationSystemGroup))]
[ExecutionSide(GameType.Client)]
[RegisterSystem("apply_gpu_camera_buffer")]
internal partial class ApplyGpuCameraBufferSystem : ASystem
{
    [ComponentQuery] private readonly Query<Camera, Position> _cameraQuery = new();

    public override Identification Identification => SystemIDs.ApplyGpuCameraBuffer;

    protected override unsafe void Execute()
    {
        if (World is null) return;

        uint[] queues = {VulkanEngine.QueueFamilyIndexes.GraphicsFamily!.Value};
        foreach (var entity in _cameraQuery)
        {
            var owner = World.EntityManager.GetEntityOwner(entity.Entity);
            if (owner != PlayerHandler.LocalPlayerGameId && owner != Constants.ServerId) continue;

            ref var camera = ref entity.GetCamera();
            var position = entity.GetPosition();

            var cameraMatrix = Matrix4x4.CreateLookAt(position.Value + camera.PositionOffset,
                position.Value + camera.PositionOffset + camera.Forward, camera.Upward);
            var camProjection = Matrix4x4.CreatePerspectiveFieldOfView(camera.Fov,
                (float) VulkanEngine.SwapchainExtent.Width / VulkanEngine.SwapchainExtent.Height, 0.1f, 200f);

            //Create the GPU data
            if (camera.GpuTransformBuffers.Length == 0)
            {
                camera.GpuTransformBuffers = new UnmanagedArray<MemoryBuffer>(VulkanEngine.SwapchainImageCount);
                camera.GpuTransformDescriptors =
                    new UnmanagedArray<DescriptorSet>(VulkanEngine.SwapchainImageCount);

                for (var i = 0; i < VulkanEngine.SwapchainImageCount; i++)
                {
                    ref var buffer = ref camera.GpuTransformBuffers[i];
                    ref var descriptor = ref camera.GpuTransformDescriptors[i];

                    buffer = MemoryBuffer.Create(BufferUsageFlags.BufferUsageUniformBufferBit,
                        (ulong) sizeof(Matrix4x4), SharingMode.Exclusive, queues.AsSpan(),
                        MemoryPropertyFlags.MemoryPropertyHostCoherentBit |
                        MemoryPropertyFlags.MemoryPropertyHostVisibleBit, false);

                    descriptor = DescriptorSetHandler.AllocateDescriptorSet(DescriptorSetIDs.CameraBuffer);

                    DescriptorBufferInfo bufferInfo = new()
                    {
                        Buffer = buffer.Buffer,
                        Offset = 0,
                        Range = (ulong) sizeof(Matrix4x4)
                    };

                    WriteDescriptorSet write = new()
                    {
                        SType = StructureType.WriteDescriptorSet,
                        PNext = null,
                        DescriptorCount = 1,
                        DescriptorType = DescriptorType.UniformBuffer,
                        DstBinding = 0,
                        DstSet = descriptor,
                        PBufferInfo = &bufferInfo
                    };

                    VulkanEngine.Vk.UpdateDescriptorSets(VulkanEngine.Device, 1, write, 0, null);
                }
            }

            var memoryBuffer = camera.GpuTransformBuffers[(int) VulkanEngine.ImageIndex];
            var matPtr = (Matrix4x4*) MemoryManager.Map(memoryBuffer.Memory);

            *matPtr = cameraMatrix * camProjection;
            MemoryManager.UnMap(memoryBuffer.Memory);
        }
    }

    public override void Setup(SystemManager systemManager)
    {
        _cameraQuery.Setup(this);
    }
}