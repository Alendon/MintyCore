using System.Numerics;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Registries;
using MintyCore.Render;
using MintyCore.Utils;
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