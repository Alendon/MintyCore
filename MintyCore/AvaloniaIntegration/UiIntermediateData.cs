using DotNext.Threading;
using MintyCore.Graphics.Render.Data;
using MintyCore.Graphics.VulkanObjects;
using MintyCore.Identifications;
using MintyCore.Registries;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.AvaloniaIntegration;

[RegisterIntermediateRenderDataByType("avalonia_ui")]
public class UiIntermediateData : IntermediateData
{
    public Texture? Texture;
    public ImageView ImageView;
    public DescriptorSet DescriptorSet;
    
    public override void Clear()
    {
        
    }

    public override Identification Identification => IntermediateRenderDataIDs.AvaloniaUi;

    public override void Dispose()
    {
        Texture?.Dispose();
        Texture = null;
    }
}