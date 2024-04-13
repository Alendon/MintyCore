using MintyCore.Graphics.Render.Data;
using MintyCore.Graphics.VulkanObjects;
using MintyCore.Identifications;
using MintyCore.Registries;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.AvaloniaIntegration;

/// <summary>
///  The intermediate data for rendering avalonia ui.
/// </summary>
[RegisterIntermediateRenderDataByType("avalonia_ui")]
public class UiIntermediateData : IntermediateData
{
    /// <summary>
    /// The copy of the texture.
    /// </summary>
    public Texture? Texture;
    /// <summary>
    ///  The image view for the texture.
    /// </summary>
    public ImageView ImageView;
    
    /// <summary>
    /// The descriptor set to bind the texture.
    /// </summary>
    public DescriptorSet DescriptorSet;

    /// <inheritdoc />
    public override void Clear()
    {
        
    }

    /// <inheritdoc />
    public override Identification Identification => IntermediateRenderDataIDs.AvaloniaUi;

    /// <inheritdoc />
    public override void Dispose()
    {
        Texture?.Dispose();
        Texture = null;
    }
}