﻿using System;
using System.Linq;
using JetBrains.Annotations;
using MintyCore.ECS;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Render;

/// <summary>
/// Render system group which automatically handles rendering
/// Use this in combination with the <see cref="ARenderSystem"/>
/// </summary>
[PublicAPI]
public abstract class ARenderSystemGroup : ASystemGroup
{
    /// <summary>
    /// The arguments for the render pass
    /// </summary>
    public RenderPassArguments RenderArguments { get; private set; }

    /// <summary>
    /// The currently active render pass. Used in <see cref="PostExecuteSystem"/>
    /// </summary>
    protected RenderPass? ActiveRenderPass { get; set; }

    /// <summary>
    /// Whether or not subpasses are used in the render pass
    /// </summary>
    protected bool UseSubpasses { get; set; }

    /// <inheritdoc />
    public override void Setup(SystemManager systemManager)
    {
        base.Setup(systemManager);

        //Check if all render systems have the render pass set to null or the next subpass bool to false
        var renderSystems = from system in Systems.Values where system is ARenderSystem select system as ARenderSystem;
        var renderSystemsArray = renderSystems.ToArray();
        var allRenderPassNull = renderSystemsArray.All(system => system.RenderArguments.RenderPass == null);
        var allNextSubPassFalse = renderSystemsArray.All(system => system.RenderArguments.NextSubpass == false);
        var allValidRenderPass =
            renderSystemsArray.All(system =>
                system.RenderArguments.RenderPass is not null || RenderArguments.RenderPass is not null);


        Logger.AssertAndThrow(allRenderPassNull || allNextSubPassFalse,
            "All render systems must have the render pass set to null or the next subpass bool to false",
            "Engine/RenderSystemGroup");

        Logger.AssertAndThrow(allValidRenderPass, "Every system must have a valid render pass",
            "Engine/RenderSystemGroup");

        UseSubpasses = !allNextSubPassFalse;
    }

    /// <inheritdoc />
    public override void PreExecuteMainThread()
    {
        ActiveRenderPass = null;

        base.PreExecuteMainThread();
    }

    /// <inheritdoc />
    protected override void PreExecuteSystem(ASystem system)
    {
        if (system is ARenderSystem renderSystem)
        {
            var cb = VulkanEngine.GetSecondaryCommandBuffer();
            renderSystem.CommandBuffer = cb;
        }

        base.PreExecuteSystem(system);
    }

    /// <inheritdoc />
    protected override void PostExecuteSystem(ASystem system)
    {
        base.PostExecuteSystem(system);

        if (system is not ARenderSystem renderSystem) return;

        if (UseSubpasses && renderSystem.RenderArguments.NextSubpass)
        {
            VulkanEngine.NextSubPass(SubpassContents.SecondaryCommandBuffers);
        }

        var renderPassToUse = RenderArguments;
        if (renderSystem.RenderArguments.RenderPass is not null)
        {
            renderPassToUse = renderSystem.RenderArguments;
        }

        if (ActiveRenderPass is null || ActiveRenderPass.Value.Handle != renderPassToUse.RenderPass!.Value.Handle)
        {
            VulkanEngine.SetActiveRenderPass(renderPassToUse.RenderPass!.Value,
                SubpassContents.SecondaryCommandBuffers, renderPassToUse.ClearValues ?? default,
                renderPassToUse.RenderArea, renderPassToUse.Framebuffer);
        }

        VulkanEngine.ExecuteSecondary(renderSystem.CommandBuffer);
    }

    /// <inheritdoc />
    public override void PostExecuteMainThread()
    {
        base.PostExecuteMainThread();

        ActiveRenderPass = null;
    }

    /// <inheritdoc />
    protected override void SetupSystem(SystemManager systemManager, ASystem system)
    {
        if (system is ARenderSystem renderSystem)
        {
            renderSystem.ParentGroup = this;
        }

        base.SetupSystem(systemManager, system);
    }

    /// <summary>
    /// Set the render pass information used by this render system group.
    /// </summary>
    protected void SetRenderPassArguments(RenderPassArguments arguments)
    {
        if (Logger.AssertAndLog(arguments.NextSubpass == false,
                "The next subpass option is not supported for render system groups", "Engine/RenderSystemGroup",
                LogImportance.Warning))
        {
            arguments.NextSubpass = false;
        }

        Logger.AssertAndLog(arguments.ClearValues is null,
            "The clear values option is not supported for render system groups", "Engine/RenderSystemGroup",
            LogImportance.Warning);

        arguments.ClearValues = Array.Empty<ClearValue>();


        RenderArguments = arguments;
    }
}