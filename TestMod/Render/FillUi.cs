using JetBrains.Annotations;
using MintyCore.Identifications;
using MintyCore.Registries;
using MintyCore.Render;
using MintyCore.Render.Managers.Interfaces;
using MintyCore.Render.Utils;
using MintyCore.Render.VulkanObjects;
using MintyCore.UI;
using Silk.NET.Vulkan;
using TestMod.Identifications;
using PipelineIDs = TestMod.Identifications.PipelineIDs;
using RenderModuleIDs = TestMod.Identifications.RenderModuleIDs;
using RenderPassIDs = TestMod.Identifications.RenderPassIDs;

namespace TestMod.Render;

[RegisterRenderModule("fill_ui")]
public class FillUi : IRenderModule
{
    public required IRenderPassManager RenderPassManager { private get; [UsedImplicitly] init; }
    public required IPipelineManager PipelineManager { private get; [UsedImplicitly] init; }
    public required IVulkanEngine VulkanEngine { private get; [UsedImplicitly] init; }
    public required IDescriptorSetManager DescriptorSetManager { private get; [UsedImplicitly] init; }
    private Vk Vk => VulkanEngine.Vk;

    ImageView[] _uiInputImageView = Array.Empty<ImageView>();
    bool[] _uiInputTexturesDirty = Array.Empty<bool>();
    Sampler _uiInputSampler = default;
    DescriptorSet[] _uiInputDescriptorSets = Array.Empty<DescriptorSet>();

    private BuildFramebuffer _framebuffer;


    public unsafe void Initialize(IRenderWorker renderWorker)
    {
        _uiInputImageView = new ImageView[VulkanEngine.SwapchainImageCount];
        _uiInputTexturesDirty = new bool[VulkanEngine.SwapchainImageCount];
        _uiInputDescriptorSets = new DescriptorSet[VulkanEngine.SwapchainImageCount];

        renderWorker.SetOutputDependencyNew(RenderModuleIDs.FillUi, RenderOutputIDs.UiRender,
            (UiRenderModule.RenderOutput input) =>
            {
                if (_uiInputImageView[VulkanEngine.ImageIndex].Handle == input.ImageView.Handle) return;

                _uiInputImageView[VulkanEngine.ImageIndex] = input.ImageView;
                _uiInputTexturesDirty[VulkanEngine.ImageIndex] = true;
            });

        renderWorker.SetInputDependencyNew<BuildFramebuffer>(RenderModuleIDs.FillUi, RenderInputIDs.BuildFramebuffer,
            input => _framebuffer = input);

        SamplerCreateInfo samplerCreateInfo = new()
        {
            SType = StructureType.SamplerCreateInfo,
            MagFilter = Filter.Linear,
            MinFilter = Filter.Linear,
            MipmapMode = SamplerMipmapMode.Linear,
            AddressModeU = SamplerAddressMode.ClampToEdge,
            AddressModeV = SamplerAddressMode.ClampToEdge,
            AddressModeW = SamplerAddressMode.ClampToEdge,
        };

        VulkanUtils.Assert(Vk.CreateSampler(VulkanEngine.Device, samplerCreateInfo, null,
            out _uiInputSampler));
    }

    /// <inheritdoc />
    public unsafe void Process(ManagedCommandBuffer cb)
    {
        if (_uiInputTexturesDirty[VulkanEngine.ImageIndex])
        {
            RecreateCurrentDescriptorSet();
        }

        if (_uiInputDescriptorSets[VulkanEngine.ImageIndex].Handle == default) return;
        
        var clearValue = new ClearValue()
        {
            Color = new ClearColorValue(0,0,0,0)
        };

        RenderPassBeginInfo beginInfo = new()
        {
            SType = StructureType.RenderPassBeginInfo,
            Framebuffer = _framebuffer.GetConcreteResult(),
            RenderPass = RenderPassManager.GetRenderPass(RenderPassIDs.Main),
            RenderArea = new Rect2D(new Offset2D(0, 0), VulkanEngine.SwapchainExtent),
            ClearValueCount = 1,
            PClearValues = &clearValue
        };

        Vk.CmdBeginRenderPass(cb, beginInfo, SubpassContents.Inline);

        var swapchainExtent = VulkanEngine.SwapchainExtent;
        var viewport = new Viewport()
        {
            Height = swapchainExtent.Height,
            Width = swapchainExtent.Width,
            MaxDepth = 1
        };
        var scissor = new Rect2D(default, swapchainExtent);

        Vk.CmdSetViewport(cb, 0, 1, viewport);
        Vk.CmdSetScissor(cb, 0, 1, scissor);
        Vk.CmdBindDescriptorSets(cb, PipelineBindPoint.Graphics, PipelineManager.GetPipelineLayout(PipelineIDs.FillUi),
            0, 1, _uiInputDescriptorSets[VulkanEngine.ImageIndex], 0, null);


        var pipeline = PipelineManager.GetPipeline(PipelineIDs.FillUi);
        Vk.CmdBindPipeline(cb, PipelineBindPoint.Graphics, pipeline);
        Vk.CmdDraw(cb, 6, 1, 0, 0);

        Vk.CmdEndRenderPass(cb);
    }

    private unsafe void RecreateCurrentDescriptorSet()
    {
        if (_uiInputDescriptorSets[VulkanEngine.ImageIndex].Handle != default)
            DescriptorSetManager.FreeDescriptorSet(_uiInputDescriptorSets[VulkanEngine.ImageIndex]);

        _uiInputDescriptorSets[VulkanEngine.ImageIndex] = DescriptorSetManager.AllocateDescriptorSet(DescriptorSetIDs.SampledTexture);

        DescriptorImageInfo imageInfo = new()
        {
            Sampler = _uiInputSampler,
            ImageLayout = ImageLayout.ShaderReadOnlyOptimal,
            ImageView = _uiInputImageView[VulkanEngine.ImageIndex]
        };

        WriteDescriptorSet write = new()
        {
            SType = StructureType.WriteDescriptorSet,
            DescriptorType = DescriptorType.CombinedImageSampler,
            DescriptorCount = 1,
            DstBinding = 0,
            DstSet = _uiInputDescriptorSets[VulkanEngine.ImageIndex],
            PImageInfo = &imageInfo
        };

        Vk.UpdateDescriptorSets(VulkanEngine.Device, 1, write, 0, null);
        
        _uiInputTexturesDirty[VulkanEngine.ImageIndex] = false;
    }

    /// <inheritdoc />
    /// <inheritdoc />
    public void Dispose()
    {
        throw new NotImplementedException();
    }
}