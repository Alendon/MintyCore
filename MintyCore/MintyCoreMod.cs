using System;
using System.Collections.Generic;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Modding.Attributes;
using MintyCore.Registries;
using MintyCore.Render;
using Silk.NET.Vulkan;

namespace MintyCore;

/// <summary>
///     The Engine/CoreGame <see cref="IMod" /> which adds all essential stuff to the game
/// </summary>
[RootMod]
public sealed partial class MintyCoreMod : IMod
{
    /// <summary />
    public MintyCoreMod()
    {
        Instance = this;
    }

    /// <summary>
    ///     The Instance of the <see cref="MintyCoreMod" />
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public static MintyCoreMod? Instance { get; private set; }

    /// <inheritdoc />
    public ushort ModId { get; set; }

    /// <inheritdoc />
    public void Dispose()
    {
    }

    internal static ModManifest ConstructManifest()
    {
        return new()
        {
            Authors = new[]
            {
                "Alendon", "Erikiller"
            },
            Version = new Version(0, 2),
            IsRootMod = true,
            Identifier = "minty_core",
            Description = "The base mod of the MintyCore engine",
            Name = "MintyCore",
            ModDependencies = new List<string>(),
            //external dependencies can be omitted
            ExternalDependencies = new List<ExternalDependency>()
        };
    }

    /// <inheritdoc />
    public void PreLoad()
    {
    }

    /// <inheritdoc />
    public void Load()
    {
        InternalRegister();
    }

    /// <inheritdoc />
    public void PostLoad()
    {
    }

    /// <inheritdoc />
    public void Unload()
    {
        InternalUnregister();
    }
    
    [RegisterExistingRenderPass("main")] internal static RenderPass MainRenderPass => RenderPassHandler.MainRenderPass;

    [RegisterRenderPass("initial")]
    internal static RenderPassInfo InitialRenderPass => new(
        new[]
        {
            new AttachmentDescription
            {
                Format = VulkanEngine.SwapchainImageFormat,
                Flags = 0,
                Samples = SampleCountFlags.Count1Bit,
                LoadOp = AttachmentLoadOp.Clear,
                StoreOp = AttachmentStoreOp.Store,
                InitialLayout = ImageLayout.Undefined,
                FinalLayout = ImageLayout.PresentSrcKhr,
                StencilLoadOp = AttachmentLoadOp.DontCare,
                StencilStoreOp = AttachmentStoreOp.DontCare
            },
            new AttachmentDescription
            {
                Format = Format.D32Sfloat,
                Samples = SampleCountFlags.Count1Bit,
                LoadOp = AttachmentLoadOp.Clear,
                StoreOp = AttachmentStoreOp.Store,
                StencilLoadOp = AttachmentLoadOp.Load,
                StencilStoreOp = AttachmentStoreOp.Store,
                InitialLayout = ImageLayout.Undefined,
                FinalLayout = ImageLayout.DepthStencilAttachmentOptimal
            }
        },
        new[]
        {
            new SubpassDescriptionInfo
            {
                Flags = 0,
                ColorAttachments = new[]
                {
                    new AttachmentReference
                    {
                        Attachment = 0,
                        Layout = ImageLayout.ColorAttachmentOptimal
                    }
                },
                InputAttachments = Array.Empty<AttachmentReference>(),
                PreserveAttachments = Array.Empty<uint>(),
                PipelineBindPoint = PipelineBindPoint.Graphics,
                HasDepthStencilAttachment = true,
                DepthStencilAttachment =
                {
                    Attachment = 1,
                    Layout = ImageLayout.DepthStencilAttachmentOptimal
                }
            }
        },
        new[]
        {
            new SubpassDependency
            {
                SrcSubpass = Vk.SubpassExternal,
                DstSubpass = 0,
                SrcStageMask = PipelineStageFlags.ColorAttachmentOutputBit,
                DstStageMask = PipelineStageFlags.ColorAttachmentOutputBit,
                SrcAccessMask = AccessFlags.NoneKhr,
                DstAccessMask = AccessFlags.ColorAttachmentWriteBit | AccessFlags.ColorAttachmentReadBit
            }
        }, 0);
    
    [RegisterDescriptorSet("sampled_texture")]
    internal static DescriptorSetInfo TextureBindInfo => new()
    {
        Bindings = new[]
        {
            new DescriptorSetLayoutBinding
            {
                Binding = 0,
                DescriptorCount = 1,
                DescriptorType = DescriptorType.CombinedImageSampler,
                StageFlags = ShaderStageFlags.FragmentBit
            }
        }
    };
    
    [RegisterDescriptorHandler("texture_fetch")]
    internal static DescriptorHandlerInfo TextureFetchInfo => new()
    {
        CategoryId = RegistryIDs.Texture,
        DescriptorFetchFunc = TextureHandler.GetTextureBindResourceSet
    };
}