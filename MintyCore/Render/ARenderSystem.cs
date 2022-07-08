using JetBrains.Annotations;
using MintyCore.ECS;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Render;

/// <summary>
/// Render system which automatically handles command buffer creation/execution and render passes.
/// Use this in combination with the <see cref="ARenderSystemGroup"/>
/// </summary>
[PublicAPI]
public abstract class ARenderSystem : ASystem
{
    /// <summary>
    /// The parent render system group.
    /// </summary>
    public ARenderSystemGroup? ParentGroup { get; internal set; }

    /// <summary>
    /// Arguments for the render pass to use
    /// </summary>
    public RenderPassArguments RenderArguments { get; internal set; }

    /// <summary>
    /// Command buffer ready to use for rendering.
    /// Access this in <see cref="ASystem.Execute"/>
    /// </summary>
    public CommandBuffer CommandBuffer { get; internal set; }


    /// <summary>
    /// Set the render information used by this render system.
    /// </summary>
    protected void SetRenderArguments(RenderPassArguments renderArguments)
    {
        Logger.AssertAndThrow(renderArguments.RenderPass is null || renderArguments.NextSubpass is false,
            $"{nameof(renderArguments.RenderPass)} and {nameof(renderArguments.NextSubpass)} cannot be both set",
            "Engine/RenderSystem");
        Logger.AssertAndLog(!(renderArguments.Framebuffer is not null && renderArguments.ClearValues is null),
            "If a custom framebuffer is provided, clear values should be provided to. (Empty Array if no clear values are used",
            "Engine/RenderSystem", LogImportance.Warning);

        RenderArguments = renderArguments;
    }
}

/// <summary>
/// Arguments to use for the render pass for the <see cref="ARenderSystem"/> and <see cref="ARenderSystemGroup"/>
/// </summary>
[PublicAPI]
public struct RenderPassArguments
{
    /// <summary>
    /// Optional framebuffer to be used.
    /// If none provided the Swapchain Framebuffer will be used.
    /// </summary>
    public Framebuffer? Framebuffer;

    /// <summary>
    /// Render pass to be used.
    /// There must be at least one available. Either through the <see cref="ARenderSystem"/> or <see cref="ARenderSystemGroup"/>
    /// </summary>
    public RenderPass? RenderPass;

    /// <summary>
    /// If true the next subpass of the parent <see cref="ARenderSystemGroup"/> render pass will be used.
    /// If this is set there must be no RenderPass provided in all <see cref="ARenderSystemGroup"/> child systems.
    /// </summary>
    public bool NextSubpass;

    /// <summary>
    /// Custom clear values to use.
    /// If a custom framebuffer is provided, this must be provided. (Empty Array if no clear values are used)
    /// This can not be used in the <see cref="ARenderSystemGroup"/>
    /// </summary>
    public ClearValue[]? ClearValues;

    /// <summary>
    /// The render area of the render pass
    /// If none is provided the default render area of the swapchain will be used.
    /// </summary>
    public Rect2D? RenderArea;
}