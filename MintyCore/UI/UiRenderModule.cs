using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MintyCore.Identifications;
using MintyCore.Render;
using MintyCore.Render.Managers.Interfaces;
using MintyCore.Render.Utils;
using MintyCore.Render.VulkanObjects;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.UI;

public class UiRenderModule : IRenderModule
{
    private IUiRenderer Renderer { get; }
    private IVulkanEngine VulkanEngine { get; }
    private IPipelineManager PipelineManager { get; }
    private IRenderPassManager RenderPassManager { get; }
    private ITextureManager TextureManager { get; }
    private IMemoryManager MemoryManager { get; }
    private IDescriptorSetManager DescriptorSetManager { get; }

    private Vk Vk => VulkanEngine.Vk;

    private Texture[] _textures = Array.Empty<Texture>();
    private ImageView[] _imageViews = Array.Empty<ImageView>();
    private ManagedFrameBuffer[] _framebuffers = Array.Empty<ManagedFrameBuffer>();
    private MemoryBuffer? _transformBuffer;
    private DescriptorSet _transformDescriptorSet;

    public UiRenderModule(IUiRenderer renderer, IVulkanEngine vulkanEngine, IPipelineManager pipelineManager,
        IRenderPassManager renderPassManager, ITextureManager textureManager, IMemoryManager memoryManager,
        IDescriptorSetManager descriptorSetManager)
    {
        Renderer = renderer;
        VulkanEngine = vulkanEngine;
        PipelineManager = pipelineManager;
        RenderPassManager = renderPassManager;
        TextureManager = textureManager;
        MemoryManager = memoryManager;
        DescriptorSetManager = descriptorSetManager;
    }

    /// <inheritdoc />
    public void Initialize(IRenderWorker renderWorker)
    {
        //renderWorker.SetOutputProviderNew(RenderModuleIDs.UiRender, RenderOutputIDs.UiRender, GetRenderOutput);

        CreateTextures();
        CreateFramebuffers();
        CreateMatrixBuffer();
    }

    private void CreateTextures()
    {
        _textures = new Texture[VulkanEngine.SwapchainImageCount];

        var textureDescription = TextureDescription.Texture2D(VulkanEngine.SwapchainExtent.Width,
            VulkanEngine.SwapchainExtent.Height,
            1, 1, Format.R8G8B8A8Unorm, TextureUsage.RenderTarget | TextureUsage.Sampled);

        for (int i = 0; i < _textures.Length; i++)
        {
            _textures[i] = TextureManager.Create(ref textureDescription);
        }
    }

    private unsafe void CreateFramebuffers()
    {
        _imageViews = new ImageView[VulkanEngine.SwapchainImageCount];
        for (var i = 0; i < _textures.Length; i++)
        {
            ImageViewCreateInfo imageViewCreateInfo = new()
            {
                SType = StructureType.ImageViewCreateInfo,
                Image = _textures[i].Image,
                SubresourceRange = new()
                {
                    AspectMask = ImageAspectFlags.ColorBit,
                    LayerCount = 1,
                    BaseArrayLayer = 0,
                    LevelCount = 1,
                    BaseMipLevel = 0
                },
                ViewType = ImageViewType.Type2D,
                Format = _textures[i].Format
            };

            VulkanUtils.Assert(Vk.CreateImageView(VulkanEngine.Device, imageViewCreateInfo, null, out _imageViews[i]));
        }

        _framebuffers = new Framebuffer[VulkanEngine.SwapchainImageCount];

        var renderPass = RenderPassManager.GetRenderPass(RenderPassIDs.UiRenderPass);

        for (var i = 0; i < _framebuffers.Length; i++)
        {
            var imageView = _imageViews[i];
            FramebufferCreateInfo createInfo = new()
            {
                SType = StructureType.FramebufferCreateInfo,
                Width = VulkanEngine.SwapchainExtent.Width,
                Height = VulkanEngine.SwapchainExtent.Height,
                Layers = 1,
                RenderPass = renderPass,
                AttachmentCount = 1,
                PAttachments = &imageView,
            };

            VulkanUtils.Assert(Vk.CreateFramebuffer(VulkanEngine.Device, createInfo, null, out _framebuffers[i]));
        }
    }

    private unsafe void CreateMatrixBuffer()
    {
        Span<uint> queue = stackalloc uint[] {VulkanEngine.QueueFamilyIndexes.GraphicsFamily!.Value};

        var stagingBuffer = MemoryManager.CreateBuffer(BufferUsageFlags.TransferSrcBit,
            (ulong) Unsafe.SizeOf<Matrix4x4>(), queue,
            MemoryPropertyFlags.HostCoherentBit | MemoryPropertyFlags.HostVisibleBit, true);

        _transformBuffer = MemoryManager.CreateBuffer(
            BufferUsageFlags.TransferDstBit | BufferUsageFlags.UniformBufferBit,
            (ulong) Unsafe.SizeOf<Matrix4x4>(), queue,
            MemoryPropertyFlags.DeviceLocalBit, false);

        var transform = Matrix4x4.CreateOrthographicOffCenter(0, VulkanEngine.SwapchainExtent.Width,
            0, VulkanEngine.SwapchainExtent.Height, 0, -1);

        Unsafe.AsRef<Matrix4x4>(MemoryManager.Map(stagingBuffer.Memory).ToPointer()) = transform;

        var cb = VulkanEngine.GetSingleTimeCommandBuffer();
        cb.CopyBuffer(stagingBuffer, _transformBuffer);
        VulkanEngine.ExecuteSingleTimeCommandBuffer(cb);
        
        stagingBuffer.Dispose();

        _transformDescriptorSet = DescriptorSetManager.AllocateDescriptorSet(DescriptorSetIDs.UiTransformBuffer);

        DescriptorBufferInfo bufferInfo = new()
        {
            Buffer = _transformBuffer.Buffer,
            Offset = 0,
            Range = _transformBuffer.Size
        };

        WriteDescriptorSet write = new()
        {
            SType = StructureType.WriteDescriptorSet,
            DescriptorCount = 1,
            DescriptorType = DescriptorType.UniformBuffer,
            DstBinding = 0,
            DstSet = _transformDescriptorSet,
            DstArrayElement = 0,
            PBufferInfo = &bufferInfo
        };

        Vk.UpdateDescriptorSets(VulkanEngine.Device, 1, write, 0, null);
    }

    /// <inheritdoc />
    public unsafe void Process(ManagedCommandBuffer cb)
    {
        var pipeline = PipelineManager.GetPipeline(PipelineIDs.UiPipeline);
        var pipelineLayout = PipelineManager.GetPipelineLayout(PipelineIDs.UiPipeline);


        using var lockHolder = Renderer.GetCurrentRenderData(out var renderData);

        Span<ClearValue> clearValues = stackalloc ClearValue[]
        {
            new ClearValue(new ClearColorValue(0, 0, 0, 1))
        };
        
        cb.BeginRenderPass(RenderPassManager.GetRenderPass(RenderPassIDs.UiRenderPass), _framebuffers[VulkanEngine.ImageIndex], clearValues, new Rect2D(default, VulkanEngine.SwapchainExtent));

        Span<DescriptorSet> descriptorBind = stackalloc DescriptorSet[]
        {
            default,
            _transformDescriptorSet
        };

        foreach (var batch in renderData)
        {
            descriptorBind[0] = batch.Texture.SampledImageDescriptorSet;
            var scissor = batch.Scissor.ToRect2D();
            var viewport = new Viewport(0, 0, VulkanEngine.SwapchainExtent.Width, VulkanEngine.SwapchainExtent.Height,
                0, 1);

            for (var index = 0; index < batch.Data.Length; index++)
            {
                Vk.CmdBindPipeline(cb, PipelineBindPoint.Graphics, pipeline);
                Vk.CmdBindDescriptorSets(cb, PipelineBindPoint.Graphics, pipelineLayout, 0, descriptorBind, default);

                Vk.CmdSetScissor(cb, 0, 1, scissor);
                Vk.CmdSetViewport(cb, 0, 1, viewport);

                Vk.CmdPushConstants(cb, pipelineLayout, ShaderStageFlags.VertexBit, 0, MemoryMarshal.AsBytes(batch.Data.Slice(index, 1)));

                Vk.CmdDraw(cb, 6, 1, 0, 0);
            }
        }

        Vk.CmdEndRenderPass(cb);
    }

    private RenderOutput GetRenderOutput()
    {
        return new RenderOutput
        {
            ImageView = _imageViews[VulkanEngine.ImageIndex]
        };
    }


    /// <inheritdoc />
    public unsafe void Dispose()
    {
        foreach (var framebuffer in _framebuffers)
        {
            Vk.DestroyFramebuffer(VulkanEngine.Device, framebuffer, null);
        }

        foreach (var imageView in _imageViews)
        {
            Vk.DestroyImageView(VulkanEngine.Device, imageView, null);
        }

        foreach (var texture in _textures)
        {
            texture.Dispose();
        }

        DescriptorSetManager.FreeDescriptorSet(_transformDescriptorSet);
        _transformBuffer?.Dispose();
    }

    public class RenderOutput
    {
        public required ImageView ImageView { get; init; }
    }
}