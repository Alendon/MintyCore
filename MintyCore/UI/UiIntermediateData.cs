using MintyCore.Graphics.Render.Data;
using MintyCore.Graphics.VulkanObjects;
using MintyCore.Identifications;
using MintyCore.Registries;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.UI;

/// <summary>
///  Represents the intermediate data for the UI renderer
/// </summary>
[RegisterIntermediateRenderDataByType("ui")]
public class UiIntermediateData : IntermediateData
{
    /// <summary>
    ///  Gets or sets the input data
    /// </summary>
    public UiRenderInputData? InputData { get; set; }
    
    /// <summary>
    ///  Gets or sets the transform buffer
    /// </summary>
    public MemoryBuffer? TransformBuffer { get; set; }
    
    /// <summary>
    ///  Gets or sets the descriptor set for the transform buffer
    /// </summary>
    public DescriptorSet TransformDescriptorSet { get; set; }
    
    /// <summary>
    ///  Gets or sets the buffer extent
    /// </summary>
    public Extent2D BufferExtent { get; set; }

    /// <inheritdoc />
    public override Identification Identification => IntermediateRenderDataIDs.Ui;

    /// <inheritdoc />
    public override void Clear()
    {
        InputData = null;
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        InputData = null;
        
        TransformBuffer?.Dispose();
        TransformBuffer = null;
    }
}