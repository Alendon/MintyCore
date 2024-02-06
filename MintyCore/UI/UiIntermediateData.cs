using MintyCore.Graphics.Render.Data;
using MintyCore.Graphics.VulkanObjects;
using MintyCore.Identifications;
using MintyCore.Registries;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.UI;

[RegisterIntermediateRenderDataByType("ui")]
public class UiIntermediateData : IntermediateData
{
    public UiRenderInputData? InputData { get; set; }
    public MemoryBuffer? TransformBuffer { get; set; }
    public DescriptorSet TransformDescriptorSet { get; set; }
    public Extent2D BufferExtent { get; set; }

    public override Identification Identification => IntermediateRenderDataIDs.Ui;
    
    public override void Clear()
    {
        InputData = null;
    }
    
    public override void Dispose()
    {
        InputData = null;
        
        TransformBuffer?.Dispose();
        TransformBuffer = null;
    }
}