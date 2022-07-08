using System;
using MintyCore.ECS;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Render;

public abstract class ARenderSystem : ASystem
{
    public ARenderSystemGroup? ParentGroup { get; internal set; }

    public RenderPassArguments RenderArguments { get; internal set; }

    public CommandBuffer CommandBuffer { get; internal set; }


    /// <summary>
    /// Set the render information used by this render system.
    /// </summary>
    protected void SetRenderArguments(RenderPassArguments renderArguments)
    {
        Logger.AssertAndThrow(renderArguments.RenderPass is null || renderArguments.NextSubpass is false,
            $"{nameof(renderArguments.RenderPass)} and {nameof(renderArguments.NextSubpass)} cannot be both set", "Engine/RenderSystem");
        Logger.AssertAndLog(!(renderArguments.Framebuffer is not null && renderArguments.ClearValues is null),
            "If a custom framebuffer is provided, clear values should be provided to. (Empty Array if no clear values are used",
            "Engine/RenderSystem", LogImportance.Warning);
        
        RenderArguments = renderArguments;
    }

    
}

public struct RenderPassArguments
{
    public Framebuffer? Framebuffer;
    public RenderPass? RenderPass;
    public bool NextSubpass;
    public ClearValue[]? ClearValues;
    public Rect2D? RenderArea;
}