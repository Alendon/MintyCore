using System.Numerics;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using MintyCore.Graphics;
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
    
    private MemoryBuffer? _lastStagingBuffer;
    
    public override void Setup()
    {
        _inputData =
            ModuleDataAccessor.UseDictionaryInputData<int, Triangle>(RenderInputDataIDs.TriangleInputData, this);

        _intermediateDataProvider =
            ModuleDataAccessor.ProvideIntermediateData<TriangleMeshData>(IntermediateRenderDataIDs.TriangleMeshData, this);
    }

    public override void Update(ManagedCommandBuffer cb)
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
    }

    public override Identification Identification => RenderInputModuleIDs.TriangleInput;
    
    private MemoryBuffer EnsureStagingBufferCapacity(int triangleCount)
    {
        throw new NotImplementedException();
    }
    
    private MemoryBuffer EnsureGpuBufferCapacity(int triangleCount, MemoryBuffer? lastBuffer)
    {
        throw new NotImplementedException();
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