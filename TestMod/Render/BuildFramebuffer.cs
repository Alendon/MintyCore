using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using MintyCore.Registries;
using MintyCore.Render;
using MintyCore.Render.Managers.Interfaces;
using MintyCore.Render.Utils;
using Silk.NET.Vulkan;
using TestMod.Identifications;

namespace TestMod.Render;

public sealed class BuildFramebuffer : IRenderInputConcreteResult<Framebuffer>
{
    private Framebuffer[] _framebuffers = Array.Empty<Framebuffer>();
    public IVulkanEngine VulkanEngine { private get; [UsedImplicitly] init; }
    public IRenderPassManager RenderPassManager { private get; [UsedImplicitly] init; }

    /// <inheritdoc />
    public void RecreateGpuData()
    {
        Dispose();
    }

    /// <inheritdoc />
    public Framebuffer GetConcreteResult()
    {
        return _framebuffers[VulkanEngine.ImageIndex];
    }

    /// <inheritdoc />
    public unsafe void Dispose()
    {
        if (_framebuffers.Length == 0) return;

        foreach (var framebuffer in _framebuffers)
        {
            VulkanEngine.Vk.DestroyFramebuffer(VulkanEngine.Device, framebuffer, null);
        }

        _framebuffers = Array.Empty<Framebuffer>();
    }

    /// <inheritdoc />
    public unsafe Task Process()
    {
        if (_framebuffers.Length != 0) return Task.CompletedTask;


        Span<ImageView> swapchainImageViews = stackalloc ImageView[1];

        var createInfo = new FramebufferCreateInfo()
        {
            SType = StructureType.FramebufferCreateInfo,
            RenderPass = RenderPassManager.GetRenderPass(RenderPassIDs.Main),
            AttachmentCount = (uint) swapchainImageViews.Length,
            PAttachments = (ImageView*) Unsafe.AsPointer(ref swapchainImageViews.GetPinnableReference()),
            Width = VulkanEngine.SwapchainExtent.Width,
            Height = VulkanEngine.SwapchainExtent.Height,
            Layers = 1
        };

        _framebuffers = new Framebuffer[VulkanEngine.SwapchainImageCount];

        for (int i = 0; i < VulkanEngine.SwapchainImageCount; i++)
        {
            swapchainImageViews[0] = VulkanEngine.SwapchainImageViews[i];

            VulkanUtils.Assert(
                VulkanEngine.Vk.CreateFramebuffer(VulkanEngine.Device, createInfo, null, out _framebuffers[i]));
        }


        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public object GetResult()
    {
        return GetConcreteResult();
    }
}