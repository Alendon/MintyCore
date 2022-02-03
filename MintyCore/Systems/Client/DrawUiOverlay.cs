using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Render;
using MintyCore.SystemGroups;
using MintyCore.UI;
using MintyCore.Utils;
using Silk.NET.Vulkan;
using SixLabors.ImageSharp.PixelFormats;

namespace MintyCore.Systems.Client;

[ExecuteInSystemGroup(typeof(PresentationSystemGroup))]
[ExecuteAfter(typeof(RenderInstancedSystem))]
public unsafe class DrawUiOverlay : ASystem
{
    Material _uiMaterial;
    private Texture _staging;
    Texture[] _uiTextures = Array.Empty<Texture>();
    private ImageView[] _uiImageViews = Array.Empty<ImageView>();
    DescriptorSet[] _uiDescriptorSets = Array.Empty<DescriptorSet>();
    private Sampler _sampler;

    private Texture _uiBorderTexture;

    private Mesh _uiOverlayMesh;

    private Extent2D Size => VulkanEngine.SwapchainExtent;

    private uint FrameIndex => VulkanEngine._imageIndex;

    private CommandBuffer _currentBuffer = default;

    public override Identification Identification => SystemIDs.DrawUiOverlay;

    public override void PreExecuteMainThread()
    {
        base.PreExecuteMainThread();
        _currentBuffer = VulkanEngine.GetSecondaryCommandBuffer();
    }

    public override void PostExecuteMainThread()
    {
        base.PostExecuteMainThread();
        VulkanEngine.ExecuteSecondary(_currentBuffer);
        _currentBuffer = default;
    }

    protected override void Execute()
    {
        CheckResize();
        DrawToStaging();
        CopyStagingToTexture();
        DrawOnScreen();
    }

    private void DrawOnScreen()
    {
        _uiMaterial.Bind(_currentBuffer);

        VulkanEngine.Vk.CmdBindDescriptorSets(_currentBuffer, PipelineBindPoint.Graphics,
            _uiMaterial.PipelineLayout, 0, 1,
            _uiDescriptorSets[FrameIndex], 0, null);

        VulkanEngine.Vk.CmdBindVertexBuffers(_currentBuffer, 0, 1, _uiOverlayMesh.MemoryBuffer.Buffer, 0);

        VulkanEngine.Vk.CmdDraw(_currentBuffer, _uiOverlayMesh.VertexCount, 1, 0, 0);
    }

    private void CheckResize()
    {
        var desc = TextureDescription.Texture2D(Size.Width, Size.Height, 1, 1, Format.R8G8B8A8Unorm,
            TextureUsage.STAGING);
        if (_staging.Width != Size.Width || _staging.Height != Size.Height)
        {
            _staging.Dispose();
            _staging = new Texture(ref desc);
        }


        if (_uiTextures[FrameIndex].Width == Size.Width && _uiTextures[FrameIndex].Height == Size.Height) return;

        VulkanEngine.Vk.DestroyImageView(VulkanEngine.Device, _uiImageViews[FrameIndex],
            VulkanEngine.AllocationCallback);
        DescriptorSetHandler.FreeDescriptorSet(_uiDescriptorSets[FrameIndex]);

        desc.Usage = TextureUsage.SAMPLED;
        _uiTextures[FrameIndex].Dispose();
        _uiTextures[FrameIndex] = new Texture(ref desc);

        RecreateCurrentImageView();

        RecreateCurrentDescriptorSet();
    }


    private void DrawToStaging()
    {

    }

    private void CopyStagingToTexture()
    {
        var cb = VulkanEngine.GetSingleTimeCommandBuffer();
        Texture.CopyTo(cb, (_staging, 0, 0, 0, 0, 0), (_uiTextures[FrameIndex], 0, 0, 0, 0, 0), Size.Width,
            Size.Height, 1, 1);
        VulkanEngine.ExecuteSingleTimeCommandBuffer(cb);
    }

    public override void Setup()
    {
        _uiMaterial = MaterialHandler.GetMaterial(MaterialIDs.UiOverlay);
        CreateInitialTextures();

        CreateInitialImageViews();

        CreateSampler();

        CreateInitialDescriptorSets();


        CreateMesh();
    }

    private void CreateMesh()
    {
        Span<Vertex> vertices = stackalloc Vertex[]
        {
            new(new Vector3(-1, 1, 0), Vector3.Zero, Vector3.Zero, new Vector2(0, 0)),
            new(new Vector3(-1, -1, 0), Vector3.Zero, Vector3.Zero, new Vector2(0, 1)),
            new(new Vector3(1, -1, 0), Vector3.Zero, Vector3.Zero, new Vector2(1, 1)),

            new(new Vector3(-1, 1, 0), Vector3.Zero, Vector3.Zero, new Vector2(0, 0)),
            new(new Vector3(1, -1, 0), Vector3.Zero, Vector3.Zero, new Vector2(1, 1)),
            new(new Vector3(1, 1, 0), Vector3.Zero, Vector3.Zero, new Vector2(1, 0)),
        };
        _uiOverlayMesh = MeshHandler.CreateDynamicMesh(vertices, (uint)vertices.Length);
    }

    private void CreateInitialDescriptorSets()
    {
        _uiDescriptorSets = new DescriptorSet[_uiTextures.Length];
        for (int i = 0; i < _uiTextures.Length; i++)
        {
            _uiDescriptorSets[i] = DescriptorSetHandler.AllocateDescriptorSet(DescriptorSetIDs.SampledTexture);

            DescriptorImageInfo imageInfo = new()
            {
                Sampler = _sampler,
                ImageLayout = ImageLayout.ShaderReadOnlyOptimal,
                ImageView = _uiImageViews[i]
            };

            WriteDescriptorSet write = new()
            {
                SType = StructureType.WriteDescriptorSet,
                DescriptorCount = 1,
                DescriptorType = DescriptorType.CombinedImageSampler,
                DstBinding = 0,
                PImageInfo = &imageInfo,
                DstSet = _uiDescriptorSets[i]
            };
            VulkanEngine.Vk.UpdateDescriptorSets(VulkanEngine.Device, 1, write, 0, null);
        }
    }

    private void CreateSampler()
    {
        SamplerCreateInfo samplerCreateInfo = new()
        {
            SType = StructureType.SamplerCreateInfo,
            Flags = 0,
            AnisotropyEnable = Vk.True,
            MaxAnisotropy = 4,
            BorderColor = BorderColor.FloatTransparentBlack,
            AddressModeU = SamplerAddressMode.Repeat,
            AddressModeV = SamplerAddressMode.Repeat,
            AddressModeW = SamplerAddressMode.Repeat,
            MinLod = 0,
            MaxLod = 1,
            MipmapMode = SamplerMipmapMode.Linear,
            MinFilter = Filter.Linear,
            MagFilter = Filter.Linear,
            CompareOp = CompareOp.Never,
            CompareEnable = Vk.True,
        };
        VulkanUtils.Assert(VulkanEngine.Vk.CreateSampler(VulkanEngine.Device, samplerCreateInfo,
            VulkanEngine.AllocationCallback, out _sampler));
    }

    private void CreateInitialImageViews()
    {
        _uiImageViews = new ImageView[_uiTextures.Length];
        for (int i = 0; i < _uiTextures.Length; i++)
        {
            var image = _uiTextures[i];
            ImageViewCreateInfo createInfo = new()
            {
                SType = StructureType.ImageViewCreateInfo,
                Image = image.Image,
                Format = Format.R8G8B8A8Unorm,
                ViewType = ImageViewType.ImageViewType2D,
                SubresourceRange = new()
                {
                    AspectMask = ImageAspectFlags.ImageAspectColorBit,
                    LayerCount = 1,
                    LevelCount = 1,
                    BaseArrayLayer = 0,
                    BaseMipLevel = 0
                }
            };
            VulkanUtils.Assert(VulkanEngine.Vk.CreateImageView(VulkanEngine.Device, createInfo,
                VulkanEngine.AllocationCallback,
                out _uiImageViews[i]));
        }
    }

    private void CreateInitialTextures()
    {
        TextureDescription description = TextureDescription.Texture2D(Size.Width, Size.Height, 1, 1,
            Format.R8G8B8A8Unorm, TextureUsage.STAGING);
        _staging = new Texture(ref description);

        description.Usage = TextureUsage.SAMPLED;
        _uiTextures = new Texture[VulkanEngine.SwapchainImageCount];
        for (int i = 0; i < _uiTextures.Length; i++)
        {
            _uiTextures[i] = new Texture(ref description);
        }
    }

    private void RecreateCurrentDescriptorSet()
    {
        _uiDescriptorSets[FrameIndex] = DescriptorSetHandler.AllocateDescriptorSet(DescriptorSetIDs.SampledTexture);

        DescriptorImageInfo imageInfo = new()
        {
            Sampler = _sampler,
            ImageLayout = ImageLayout.ShaderReadOnlyOptimal,
            ImageView = _uiImageViews[FrameIndex]
        };

        WriteDescriptorSet write = new()
        {
            SType = StructureType.WriteDescriptorSet,
            DescriptorCount = 1,
            DescriptorType = DescriptorType.CombinedImageSampler,
            DstBinding = 0,
            PImageInfo = &imageInfo,
            DstSet = _uiDescriptorSets[FrameIndex]
        };
        VulkanEngine.Vk.UpdateDescriptorSets(VulkanEngine.Device, 1, write, 0, null);
    }

    private void RecreateCurrentImageView()
    {
        ImageViewCreateInfo createInfo = new()
        {
            SType = StructureType.ImageViewCreateInfo,
            Image = _uiTextures[FrameIndex].Image,
            Format = Format.R8G8B8A8Unorm,
            ViewType = ImageViewType.ImageViewType2D,
            SubresourceRange = new()
            {
                AspectMask = ImageAspectFlags.ImageAspectColorBit,
                LayerCount = 1,
                LevelCount = 1,
                BaseArrayLayer = 0,
                BaseMipLevel = 0
            }
        };
        VulkanUtils.Assert(VulkanEngine.Vk.CreateImageView(VulkanEngine.Device, createInfo,
            VulkanEngine.AllocationCallback,
            out _uiImageViews[FrameIndex]));
    }

    public override void Dispose()
    {
        _uiOverlayMesh.Dispose();


        foreach (var imageView in _uiImageViews)
        {
            VulkanEngine.Vk.DestroyImageView(VulkanEngine.Device, imageView, VulkanEngine.AllocationCallback);
        }

        foreach (var uiDescriptorSet in _uiDescriptorSets)
        {
            DescriptorSetHandler.FreeDescriptorSet(uiDescriptorSet);
        }

        VulkanEngine.Vk.DestroySampler(VulkanEngine.Device, _sampler, VulkanEngine.AllocationCallback);

        _staging.Dispose();
        foreach (var texture in _uiTextures)
        {
            texture.Dispose();
        }

        _uiBorderTexture.Dispose();
    }
}