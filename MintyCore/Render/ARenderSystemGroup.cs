using System.Linq;
using MintyCore.ECS;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Render;

public abstract class ARenderSystemGroup : ASystemGroup
{
    public ARenderSystemGroup? ParentGroup { get; internal set; }

    public RenderPassArguments RenderArguments { get; private set; }

    protected RenderPass? ActiveRenderPass { get; set; }
    protected bool UseSubpasses { get; set; }

    public override void Setup(SystemManager systemManager)
    {
        base.Setup(systemManager);

        //Check if all render systems have the render pass set to null or the next subpass bool to false
        var renderSystems = from system in Systems.Values where system is ARenderSystem select system as ARenderSystem;
        var renderSystemsArray = renderSystems.ToArray();
        bool allRenderPassNull = renderSystemsArray.All(system => system.RenderArguments.RenderPass == null);
        bool allNextSubPassFalse = renderSystemsArray.All(system => system.RenderArguments.NextSubpass == false);
        bool allValidRenderPass =
            renderSystemsArray.All(system => system.RenderArguments.RenderPass is not null || RenderArguments.RenderPass is not null);


        Logger.AssertAndThrow(allRenderPassNull || allNextSubPassFalse,
            "All render systems must have the render pass set to null or the next subpass bool to false",
            "Engine/RenderSystemGroup");
        
        Logger.AssertAndThrow(allValidRenderPass, "Every system must have a valid render pass", "Engine/RenderSystemGroup");

        UseSubpasses = !allNextSubPassFalse;
    }

    public override void PreExecuteMainThread()
    {
        ActiveRenderPass = null;
        
        base.PreExecuteMainThread();
    }

    protected override void PreExecuteSystem(ASystem system)
    {
        if (system is ARenderSystem renderSystem)
        {
            var cb = VulkanEngine.GetSecondaryCommandBuffer();
            renderSystem.CommandBuffer = cb;
        }

        base.PreExecuteSystem(system);
    }

    protected override void PostExecuteSystem(ASystem system)
    {
        base.PostExecuteSystem(system);

        if (system is ARenderSystem renderSystem)
        {
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
    }

    public override void PostExecuteMainThread()
    {
        base.PostExecuteMainThread();

        ActiveRenderPass = null;
    }

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
    /// <param name="renderPass">RenderPass to use, if null all child systems have to provide their own render pass</param>
    protected void SetRenderPassArguments(RenderPassArguments arguments)
    {
        if (Logger.AssertAndLog(arguments.NextSubpass == false,
                "The next subpass option is not supported for render system groups", "Engine/RenderSystemGroup",
                LogImportance.Warning))
        {
            arguments.NextSubpass = false;
        }

        if (Logger.AssertAndLog(arguments.ClearValues is null,
                "The clear values option is not supported for render system groups", "Engine/RenderSystemGroup",
                LogImportance.Warning))
        {
            arguments.ClearValues = null;
        }
        
        RenderArguments = arguments;
    }
}