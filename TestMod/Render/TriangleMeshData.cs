using MintyCore.Graphics.Render.Data;
using MintyCore.Graphics.VulkanObjects;
using MintyCore.Registries;
using MintyCore.Utils;
using Silk.NET.Vulkan;
using TestMod.Identifications;

namespace TestMod.Render;

[RegisterIntermediateRenderDataByType("triangle_mesh_data")]
public class TriangleMeshData : IntermediateData
{
    public int TriangleCount { get; set; }

    public MemoryBuffer? TriangleBuffer { get; set; }
    public DescriptorSet BufferDescriptor { get; set; }

    public override void Clear()
    {
        TriangleCount = 0;
    }

    public override Identification Identification => IntermediateRenderDataIDs.TriangleMeshData;

    public override void Dispose()
    {
        TriangleBuffer?.Dispose();
    }
}