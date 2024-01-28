using System;
using System.Linq;
using JetBrains.Annotations;
using MintyCore.Registries;
using MintyCore.Render;
using MintyCore.Render.Managers.Interfaces;
using MintyCore.Render.Utils;
using MintyCore.Render.VulkanObjects;
using OneOf;
using Silk.NET.Vulkan;

namespace MintyCore.Graphics.RenderGraphTest.RenderResources;

[PublicAPI]
public class TextureResource : IRenderResource
{
    private ITextureManager _textureManager;
    private IVulkanEngine _vulkanEngine;

    private TextureResource(ITextureManager textureManager, IVulkanEngine vulkanEngine, Texture[] internalTextures,
        Texture? internalStagingTexture, ImageView[] imageViews)
    {
        _textureManager = textureManager;
        _vulkanEngine = vulkanEngine;
        InternalTextures = internalTextures;
        InternalStagingTexture = internalStagingTexture;
        ImageViews = imageViews;
    }

    private bool _isSwapchain;
    public Texture[] InternalTextures { get; }
    public ImageView[] ImageViews { get; }
    public Texture? InternalStagingTexture { get; }

    public static unsafe TextureResource Create(
        TextureResourceDescription description,
        ITextureManager textureManager,
        IVulkanEngine vulkanEngine
    )
    {
        var size = description.InitialSize.Match(
            direct => direct,
            getter => getter()
        );

        var format = description.InitialFormat.Match(
            direct => direct,
            getter => getter()
        );
        var textureDescription = TextureDescription.Texture2D(size.Width, size.Height,
            1, 1, format, description.TextureUsage);

        var textures = Enumerable.Range(0, 3).Select(_ => textureManager.Create(ref textureDescription)).ToArray();


        Texture? stagingTexture = null;

        if (!description.GpuOnly)
        {
            var stagingTextureDescription = TextureDescription.Texture2D(size.Width, size.Height,
                1, 1, format, TextureUsage.Staging);
            stagingTexture = textureManager.Create(ref stagingTextureDescription);
        }

        var imageViewCreateInfo = new ImageViewCreateInfo
        {
            SType = StructureType.ImageViewCreateInfo,
            Format = textures[0].Format,
            SubresourceRange = new ImageSubresourceRange
            {
                AspectMask = ImageAspectFlags.ColorBit,
                LayerCount = 1,
                LevelCount = 1,
                BaseArrayLayer = 0,
                BaseMipLevel = 0
            },
            ViewType = ImageViewType.Type2D
        };
        var imageViews = new ImageView[textures.Length];
        for (var i = 0; i < textures.Length; i++)
        {
            imageViewCreateInfo.Image = textures[i].Image;
            VulkanUtils.Assert(vulkanEngine.Vk.CreateImageView(vulkanEngine.Device, in imageViewCreateInfo, null,
                out imageViews[i]));
        }

        return new TextureResource(textureManager, vulkanEngine, textures, stagingTexture, imageViews);
    }

    internal static TextureResource CreateFromSwapchain(ITextureManager textureManager,
        IVulkanEngine vulkanEngine, IMemoryManager memoryManager)
    {
        var textures = new Texture[vulkanEngine.SwapchainImageCount];
        var imageViews = new ImageView[vulkanEngine.SwapchainImageCount];

        for (int i = 0; i < textures.Length; i++)
        {
            textures[i] = new Texture(vulkanEngine, memoryManager, vulkanEngine.SwapchainImages[i],
                default, default, vulkanEngine.SwapchainImageFormat, vulkanEngine.SwapchainExtent.Width,
                vulkanEngine.SwapchainExtent.Height, 1, 1, 1, TextureUsage.RenderTarget, ImageType.Type2D,
                SampleCountFlags.Count1Bit, new [] { ImageLayout.Undefined }, true);
            
            imageViews[i] = vulkanEngine.SwapchainImageViews[i];
        }
        
        var result = new TextureResource(textureManager, vulkanEngine, textures, null, imageViews);
        result._isSwapchain = true;
        return result;
    }


    /// <inheritdoc />
    public unsafe void Dispose()
    {
        if (_isSwapchain)
        {
            return;
        }
        
        foreach (var imageView in ImageViews)
        {
            _vulkanEngine.Vk.DestroyImageView(_vulkanEngine.Device, imageView, null);
        }
        
        InternalStagingTexture?.Dispose();
        foreach (var internalTexture in InternalTextures)
        {
            internalTexture.Dispose();
        }
    }

    [RegisterSwapchainResourceDoNotUseThis("swapchain")]
    internal static object Swapchain => new();
}

public record TextureResourceDescription(
    OneOf<Extent2D, Func<Extent2D>> InitialSize,
    OneOf<Format, Func<Format>> InitialFormat,
    bool GpuOnly,
    TextureUsage TextureUsage
);