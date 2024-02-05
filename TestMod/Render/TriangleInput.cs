using System.Numerics;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using MintyCore.Graphics;
using MintyCore.Graphics.Managers;
using MintyCore.Graphics.Render;
using MintyCore.Graphics.Render.Data;
using MintyCore.Graphics.Render.Data.RegistryWrapper;
using MintyCore.Graphics.VulkanObjects;
using MintyCore.Registries;
using MintyCore.Utils;
using OneOf;
using Silk.NET.Vulkan;
using TestMod.Identifications;

namespace TestMod.Render;

[RegisterInputDataModule("triangle_input")]
public class TriangleInput : InputModule
{
    private DictionaryInputData<int, Triangle>? _inputData;
    private Func<TriangleMeshData>? _intermediateDataProvider;

    public required IVulkanEngine VulkanEngine { private get; [UsedImplicitly] set; }
    public required IDescriptorSetManager DescriptorSetManager { private get; [UsedImplicitly] set; }
    public required IMemoryManager MemoryManager { private get; [UsedImplicitly] set; }

    private MemoryBuffer? _lastStagingBuffer;

    public override void Setup()
    {
        _inputData =
            ModuleDataAccessor.UseDictionaryInputData<int, Triangle>(RenderInputDataIDs.TriangleInputData, this);

        _intermediateDataProvider =
            ModuleDataAccessor.ProvideIntermediateData<TriangleMeshData>(IntermediateRenderDataIDs.TriangleMeshData,
                this);
    }

    public override unsafe void Update(ManagedCommandBuffer cb)
    {
        var triangles = _inputData.AcquireData();
        var triangleMeshData = _intermediateDataProvider();

        var triangleStagingBuffer = EnsureStagingBufferCapacity(triangles.Count);
        var triangleGpuBuffer = EnsureGpuBufferCapacity(triangles.Count, triangleMeshData.TriangleBuffer);

        var stagingData = triangleStagingBuffer.MapAs<Triangle>();

        var i = 0;
        foreach (var (_, triangle) in triangles.OrderBy(x => x.Key))
        {
            stagingData[i++] = triangle;
        }

        //Technically unmap is not needed on a staging buffer, but it's good practice to do so
        triangleStagingBuffer.Unmap();

        var copy = new BufferCopy
        {
            Size = (ulong)(triangles.Count * Marshal.SizeOf<Triangle>())
        };

        cb.CopyBuffer(triangleStagingBuffer, triangleGpuBuffer, copy);

        //Apply the changes
        triangleMeshData.TriangleBuffer = triangleGpuBuffer;
        triangleMeshData.TriangleCount = triangles.Count;

        _lastStagingBuffer = triangleStagingBuffer;

        if (triangleMeshData.BufferDescriptor.Handle == default)
            triangleMeshData.BufferDescriptor = DescriptorSetManager.AllocateDescriptorSet(DescriptorSetIDs.BufferBind);

        var descriptor = triangleMeshData.BufferDescriptor;

        var bufferInfo = new DescriptorBufferInfo
        {
            Buffer = triangleGpuBuffer.Buffer,
            Offset = 0,
            Range = (ulong)(triangles.Count * Marshal.SizeOf<Triangle>())
        };

        var write = new WriteDescriptorSet
        {
            SType = StructureType.WriteDescriptorSet,
            DescriptorCount = 1,
            DescriptorType = DescriptorType.StorageBuffer,
            DstSet = descriptor,
            PBufferInfo = &bufferInfo
        };

        VulkanEngine.Vk.UpdateDescriptorSets(VulkanEngine.Device, 1, write, 0, null);
    }

    public override Identification Identification => RenderInputModuleIDs.TriangleInput;

    private MemoryBuffer EnsureStagingBufferCapacity(int triangleCount)
    {
        if (_lastStagingBuffer is not null &&
            _lastStagingBuffer.Size >= (ulong)(triangleCount * Marshal.SizeOf<Triangle>()))
            return _lastStagingBuffer;

        _lastStagingBuffer?.Dispose();

        Span<uint> queueIndices =
        [
            VulkanEngine.GraphicQueue.familyIndex
        ];

        return MemoryManager.CreateBuffer(BufferUsageFlags.TransferSrcBit,
            (ulong)(triangleCount * Marshal.SizeOf<Triangle>()), queueIndices,
            MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit, true);
    }

    private MemoryBuffer EnsureGpuBufferCapacity(int triangleCount, MemoryBuffer? lastBuffer)
    {
        if (lastBuffer is not null && lastBuffer.Size >= (ulong)(triangleCount * Marshal.SizeOf<Triangle>()))
            return lastBuffer;

        lastBuffer?.Dispose();

        Span<uint> queueIndices =
        [
            VulkanEngine.GraphicQueue.familyIndex
        ];

        return MemoryManager.CreateBuffer(BufferUsageFlags.StorageBufferBit | BufferUsageFlags.TransferDstBit,
            (ulong)(triangleCount * Marshal.SizeOf<Triangle>()), queueIndices, MemoryPropertyFlags.DeviceLocalBit,
            false);
    }


    public override void Dispose()
    {
        _lastStagingBuffer?.Dispose();
    }

    [RegisterKeyIndexedInputData("triangle_input_data")]
    public static DictionaryInputDataRegistryWrapper<int, Triangle> TriangleData => new();
}

[StructLayout(LayoutKind.Explicit)]
public struct Triangle
{
    [field: FieldOffset(4 * 4 * 0)] public Vector3 Point1 { get; init; }

    [field: FieldOffset(4 * 4 * 1)] public Vector3 Point2 { get; init; }

    [field: FieldOffset(4 * 4 * 2)] public Vector3 Point3 { get; init; }

    [field: FieldOffset(4 * 4 * 3)] public Vector3 Color { get; init; }
}