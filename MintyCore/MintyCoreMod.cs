using System;
using System.Collections.Generic;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Modding.Attributes;
using MintyCore.Registries;
using MintyCore.Render;
using MintyCore.Render.Managers;
using MintyCore.Render.Managers.Interfaces;
using Silk.NET.Vulkan;

namespace MintyCore;

/// <summary>
///     The Engine/CoreGame <see cref="IMod" /> which adds all essential stuff to the game
/// </summary>
public sealed class MintyCoreMod : IMod
{
    /// <inheritdoc />
    public void Dispose()
    {
    }

    internal static ModManifest ConstructManifest()
    {
        return new ModManifest
        {
            Authors = new[]
            {
                "Alendon", "Erikiller"
            },
            Version = new Version(0, 5),
            IsRootMod = true,
            Identifier = "minty_core",
            Description = "The base mod of the MintyCore engine",
            Name = "MintyCore",
            ModDependencies = Array.Empty<string>(),
            //external dependencies can be omitted
            ExternalDependencies = Array.Empty<ExternalDependency>()
        };
    }

    /// <inheritdoc />
    public void PreLoad()
    {
    }

    /// <inheritdoc />
    public void Load()
    {
    }

    /// <inheritdoc />
    public void PostLoad()
    {
    }

    /// <inheritdoc />
    public void Unload()
    {
    }

    [RegisterExistingRenderPass("main")]
    internal static RenderPass GetMainRenderPass(IRenderPassManager renderPassManager) => renderPassManager.MainRenderPass;

    [RegisterRenderPass("initial")]
    internal static RenderPassInfo GetInitialRenderPass(IVulkanEngine vulkanEngine) =>
        new(
            new[]
            {
                new AttachmentDescription
                {
                    Format = vulkanEngine.SwapchainImageFormat,
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
        },
        DescriptorSetsPerPool = 100
    };
}