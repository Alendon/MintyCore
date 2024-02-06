using MintyCore.Registries;
using Silk.NET.Vulkan;

namespace TestMod.Render;

public static class RenderObjects
{
    [RegisterDescriptorSet("buffer_bind")]
    public static DescriptorSetInfo BufferBind => new()
    {
        Bindings =
        [
            new DescriptorSetLayoutBinding()
            {
                Binding = 0,
                DescriptorCount = 1,
                DescriptorType = DescriptorType.StorageBuffer,
                StageFlags = ShaderStageFlags.VertexBit
            }
        ],
        DescriptorSetsPerPool = 100
    };
}