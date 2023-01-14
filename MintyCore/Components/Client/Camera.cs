using System;
using System.Numerics;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Registries;
using MintyCore.Render;
using MintyCore.Utils;
using MintyCore.Utils.Maths;
using MintyCore.Utils.UnmanagedContainers;
using Silk.NET.Vulkan;

namespace MintyCore.Components.Client;

/// <summary>
///     Component to track camera data
/// </summary>
[PlayerControlled]
[RegisterComponent("camera")]
public struct Camera : IComponent
{
    /// <inheritdoc />
    public bool Dirty { get; set; }

    /// <summary>
    ///     Stores the field of view
    /// </summary>
    public float Fov;

    /// <summary>
    ///     Position Offset from the entity Position
    /// </summary>
    public Vector3 PositionOffset;

    /// <summary>
    ///     The Forward Vector of the camera
    /// </summary>
    public Vector3 Forward;

    /// <summary>
    ///     The Upward Vector of the camera
    /// </summary>
    public Vector3 Upward;

    /// <summary>
    /// 
    /// </summary>
    public float NearPlane;
    
    /// <summary>
    /// 
    /// </summary>
    public float FarPlane;

    /// <summary>
    ///     <see cref="Identification" /> of the <see cref="Camera" /> Component
    /// </summary>
    public Identification Identification => ComponentIDs.Camera;

    /// <summary>
    ///     Transform buffers of the camera. One per swapchain image
    /// </summary>
    public UnmanagedArray<MemoryBuffer> GpuTransformBuffers;

    /// <summary>
    ///     <see cref="DescriptorSet" /> for the transform buffer. One per swapchain image
    /// </summary>
    public UnmanagedArray<DescriptorSet> GpuTransformDescriptors;

    /// <inheritdoc />
    public bool Deserialize(DataReader reader, IWorld world, Entity entity)
    {
        if (!reader.TryGetFloat(out var fov)) return false;
        Fov = fov;
        return true;
    }

    /// <inheritdoc />
    public void PopulateWithDefaultValues()
    {
        Fov = 1.0f;
        PositionOffset = Vector3.Zero;
        Forward = new Vector3(0, 0, 1);
        Upward = new Vector3(0, -1, 0);
        NearPlane = 0.1f;
        FarPlane = 1000.0f;
        CreateGpuData();
    }

    private unsafe void CreateGpuData()
    {
        if (!MathHelper.IsBitSet((int) Engine.GameType, (int) GameType.Client)) return;

        GpuTransformBuffers = new UnmanagedArray<MemoryBuffer>(VulkanEngine.SwapchainImageCount);
        GpuTransformDescriptors = new UnmanagedArray<DescriptorSet>(VulkanEngine.SwapchainImageCount);
        uint[] queues = {VulkanEngine.QueueFamilyIndexes.GraphicsFamily!.Value};

        for (int i = 0; i < VulkanEngine.SwapchainImageCount; i++)
        {
            ref var buffer = ref GpuTransformBuffers[i];
            ref var descriptor = ref GpuTransformDescriptors[i];

            buffer = MemoryBuffer.Create(BufferUsageFlags.UniformBufferBit,
                (ulong) sizeof(Matrix4x4), SharingMode.Exclusive, queues.AsSpan(),
                MemoryPropertyFlags.HostCoherentBit |
                MemoryPropertyFlags.HostVisibleBit, false);

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

    /// <inheritdoc />
    public void Serialize(DataWriter writer, IWorld world, Entity entity)
    {
        writer.Put(Fov);
    }

    /// <inheritdoc />
    public void IncreaseRefCount()
    {
    }


    /// <inheritdoc />
    public void DecreaseRefCount()
    {
        foreach (var buffer in GpuTransformBuffers) buffer.Dispose();

        foreach (var descriptor in GpuTransformDescriptors) DescriptorSetHandler.FreeDescriptorSet(descriptor);

        while (!GpuTransformBuffers.DecreaseRefCount())
        {
        }

        while (!GpuTransformDescriptors.DecreaseRefCount())
        {
        }

        GpuTransformBuffers = default;
        GpuTransformDescriptors = default;
    }
}