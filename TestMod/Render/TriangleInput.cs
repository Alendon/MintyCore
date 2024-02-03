using System.Numerics;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using MintyCore.Graphics.Render;
using MintyCore.Graphics.Render.Data;
using MintyCore.Graphics.Render.Data.RegistryWrapper;
using MintyCore.Registries;
using MintyCore.Render;
using MintyCore.Render.Managers.Interfaces;
using MintyCore.Render.VulkanObjects;
using MintyCore.Utils;
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
            ModuleDataAccessor.UseIntermediateData<TriangleMeshData>(IntermediateRenderDataIDs.TriangleMeshData, this);
    }

    public override void Update(CommandBuffer cb)
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
        
        VulkanEngine.Vk.CmdCopyBuffer(cb, triangleStagingBuffer.Buffer, triangleGpuBuffer.Buffer, 1, copy);
        
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