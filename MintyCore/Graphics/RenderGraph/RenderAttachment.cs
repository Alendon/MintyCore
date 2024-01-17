using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Graphics.RenderGraph;

public record RenderAttachment(
    Identification TextureResource,
    AttachmentLoadOp LoadOperation,
    AttachmentStoreOp StoreOperation,
    ClearValue ClearValue
);