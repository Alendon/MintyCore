using JetBrains.Annotations;
using OneOf;
using OneOf.Types;
using Silk.NET.Vulkan;

namespace MintyCore.Graphics.RenderGraphTest;

[PublicAPI]
public record RenderInfo
(
    OneOf<Rect2D, RenderInfo.FullArea> RenderArea,
    OneOf<RenderAttachment, RenderAttachment[], None> ColorAttachments,
    OneOf<RenderAttachment, None> DepthAttachment,
    OneOf<RenderAttachment, None> StencilAttachment
    )
{



    public struct FullArea {}
}