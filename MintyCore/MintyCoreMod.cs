using System;
using System.Numerics;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Modding.Attributes;
using MintyCore.Registries;
using MintyCore.Render;
using MintyCore.UI;
using MintyCore.Utils;
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

    /// <inheritdoc />
    public string StringIdentifier => "minty_core";

    /// <inheritdoc />
    public string ModDescription => "The base mod of the MintyCore engine";

    /// <inheritdoc />
    public string ModName => "MintyCore";

    /// <inheritdoc />
    public ModVersion ModVersion => new(0, 0, 2);

    /// <inheritdoc />
    public ModDependency[] ModDependencies => Array.Empty<ModDependency>();

    /// <inheritdoc />
    public GameType ExecutionSide => GameType.Local;

    /// <inheritdoc />
    public void PreLoad()
    {
    }

    [OverrideWorld("default", "minty_core")]
    public static WorldInfo TechardryWorldInfo => new()
    {
        WorldCreateFunction = serverWorld => new World(serverWorld),
    };

    /// <inheritdoc />
    public void Load()
    {
        InternalRegister();
    }

    [RegisterWorld("default")]
    internal static WorldInfo DefaultWorld => new()
    {
        WorldCreateFunction = server => new World(server)
    };

    /// <inheritdoc />
    public void PostLoad()
    {
    }

    /// <inheritdoc />
    public void Unload()
    {
        InternalUnregister();
    }

    [RegisterUiPrefab("main_menu_prefab")]
    internal static PrefabElementInfo MainMenuPrefabElement => new()
    {
        PrefabCreator = () => new MainMenu()
    };

    [RegisterUiRoot("main_menu")]
    internal static RootElementInfo MainMenuRoot => new()
    {
        RootElementPrefab = UiIDs.MainMenuPrefab
    };

    [RegisterFontFamily("akashi", "akashi.ttf")]
    internal static FontInfo Akashi => default;

    [RegisterArchetype("test_render")]
    internal static ArchetypeInfo TestRender => new()
    {
        EntitySetup = null,
        ComponentIDs = new[]
        {
            ComponentIDs.InstancedRenderAble, ComponentIDs.Transform, ComponentIDs.Position,
            ComponentIDs.Rotation, ComponentIDs.Scale
        },
        AdditionalDlls = new[]
        {
            typeof(DescriptorSet).Assembly.Location
        }
    };

    [RegisterInstancedRenderData("testing")]
    internal static InstancedRenderDataInfo Testing => new()
    {
        MaterialIds = new[]
        {
            MaterialIDs.GroundTexture
        },
        MeshId = MeshIDs.Cube
    };

    [RegisterDescriptorSet("camera_buffer")]
    internal static DescriptorSetInfo CameraBufferInfo => new()
    {
        Bindings = new[]
        {
            new DescriptorSetLayoutBinding
            {
                Binding = 0,
                DescriptorCount = 1,
                DescriptorType = DescriptorType.UniformBuffer,
                StageFlags = ShaderStageFlags.ShaderStageVertexBit
            }
        }
    };

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
                StageFlags = ShaderStageFlags.ShaderStageFragmentBit
            }
        }
    };

    [RegisterDescriptorHandler("texture_fetch")]
    internal static DescriptorHandlerInfo TextureFetchInfo => new()
    {
        CategoryId = RegistryIDs.Texture,
        DescriptorFetchFunc = id => TextureHandler.GetTextureBindResourceSet(id)
    };

    [RegisterMaterial("triangle")]
    internal static MaterialInfo TriangleInfo => new()
    {
        PipelineId = PipelineIDs.Color,
        DescriptorSets = Array.Empty<(Identification, uint)>()
    };

    [RegisterMaterial("ground_texture")]
    internal static MaterialInfo GroundInfo => new()
    {
        PipelineId = PipelineIDs.Texture,
        DescriptorSets = new[]
        {
            (TextureIDs.Ground, 1u)
        }
    };

    [RegisterMaterial("ui_overlay")]
    internal static MaterialInfo UiOverlayInfo => new()
    {
        PipelineId = PipelineIDs.UiOverlay,
        DescriptorSets = Array.Empty<(Identification, uint)>()
    };

    [RegisterExistingRenderPass("main")] internal static RenderPass MainRenderPass => RenderPassHandler.MainRenderPass;

    [RegisterRenderPass("initial")]
    internal static RenderPassInfo InitialRenderPass => new RenderPassInfo(
        new[]
        {
            new AttachmentDescription()
            {
                Format = VulkanEngine.SwapchainImageFormat,
                Flags = 0,
                Samples = SampleCountFlags.SampleCount1Bit,
                LoadOp = AttachmentLoadOp.Clear,
                StoreOp = AttachmentStoreOp.Store,
                InitialLayout = ImageLayout.Undefined,
                FinalLayout = ImageLayout.PresentSrcKhr,
                StencilLoadOp = AttachmentLoadOp.DontCare,
                StencilStoreOp = AttachmentStoreOp.DontCare
            },
            new AttachmentDescription()
            {
                Format = Format.D32Sfloat,
                Samples = SampleCountFlags.SampleCount1Bit,
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
            new SubpassDescriptionInfo()
            {
                Flags = 0,
                ColorAttachments = new[]
                {
                    new AttachmentReference()
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
            new SubpassDependency()
            {
                SrcSubpass = Vk.SubpassExternal,
                DstSubpass = 0,
                SrcStageMask = PipelineStageFlags.PipelineStageColorAttachmentOutputBit,
                DstStageMask = PipelineStageFlags.PipelineStageColorAttachmentOutputBit,
                SrcAccessMask = AccessFlags.AccessNoneKhr,
                DstAccessMask = AccessFlags.AccessColorAttachmentWriteBit | AccessFlags.AccessColorAttachmentReadBit
            }
        }, 0);
        [RegisterGraphicsPipeline("color")]
    internal static unsafe GraphicsPipelineDescription ColorDescription
    {
        get
        {
            Rect2D scissor = new()
            {
                Extent = VulkanEngine.SwapchainExtent,
                Offset = new Offset2D(0, 0)
            };
            Viewport viewport = new()
            {
                Width = VulkanEngine.SwapchainExtent.Width,
                Height = VulkanEngine.SwapchainExtent.Height,
                MaxDepth = 1f,
                MinDepth = 0f
            };

            var vertexInputBindings = new[]
            {
                Vertex.GetVertexBinding(),
                new VertexInputBindingDescription
                {
                    Binding = 1,
                    Stride = (uint) sizeof(Matrix4x4),
                    InputRate = VertexInputRate.Instance
                }
            };

            var attributes = Vertex.GetVertexAttributes();
            var vertexInputAttributes =
                new VertexInputAttributeDescription[attributes.Length + 4];
            for (var i = 0; i < attributes.Length; i++) vertexInputAttributes[i] = attributes[i];

            vertexInputAttributes[attributes.Length] = new VertexInputAttributeDescription
            {
                Binding = 1,
                Format = Format.R32G32B32A32Sfloat,
                Location = (uint) attributes.Length,
                Offset = 0
            };
            vertexInputAttributes[attributes.Length + 1] = new VertexInputAttributeDescription
            {
                Binding = 1,
                Format = Format.R32G32B32A32Sfloat,
                Location = (uint) attributes.Length + 1,
                Offset = (uint) sizeof(Vector4)
            };
            vertexInputAttributes[attributes.Length + 2] = new VertexInputAttributeDescription
            {
                Binding = 1,
                Format = Format.R32G32B32A32Sfloat,
                Location = (uint) attributes.Length + 2,
                Offset = (uint) sizeof(Vector4) * 2
            };
            vertexInputAttributes[attributes.Length + 3] = new VertexInputAttributeDescription
            {
                Binding = 1,
                Format = Format.R32G32B32A32Sfloat,
                Location = (uint) attributes.Length + 3,
                Offset = (uint) sizeof(Vector4) * 3
            };

            var colorBlendAttachment = new[]
            {
                new PipelineColorBlendAttachmentState
                {
                    BlendEnable = Vk.True,
                    SrcColorBlendFactor = BlendFactor.SrcAlpha,
                    DstColorBlendFactor = BlendFactor.OneMinusSrcAlpha,
                    ColorBlendOp = BlendOp.Add,
                    SrcAlphaBlendFactor = BlendFactor.One,
                    DstAlphaBlendFactor = BlendFactor.Zero,
                    AlphaBlendOp = BlendOp.Add,
                    ColorWriteMask = ColorComponentFlags.ColorComponentRBit |
                                     ColorComponentFlags.ColorComponentGBit |
                                     ColorComponentFlags.ColorComponentBBit | ColorComponentFlags.ColorComponentABit
                }
            };

            var dynamicStates = new[]
            {
                DynamicState.Viewport,
                DynamicState.Scissor
            };

            GraphicsPipelineDescription pipelineDescription = new()
            {
                Shaders = new[]
                {
                    ShaderIDs.TriangleVert,
                    ShaderIDs.ColorFrag
                },
                Scissors = new[] {scissor},
                Viewports = new[] {viewport},
                DescriptorSets = new[] {DescriptorSetIDs.CameraBuffer},
                Flags = 0,
                Topology = PrimitiveTopology.TriangleList,
                DynamicStates = dynamicStates,
                RenderPass = RenderPassIDs.Main,
                SampleCount = SampleCountFlags.SampleCount1Bit,
                SubPass = 0,
                BasePipelineHandle = default,
                BasePipelineIndex = 0,
                PrimitiveRestartEnable = false,
                AlphaToCoverageEnable = false,
                VertexAttributeDescriptions = vertexInputAttributes,
                VertexInputBindingDescriptions = vertexInputBindings,
                RasterizationInfo =
                {
                    CullMode = CullModeFlags.CullModeNone,
                    FrontFace = FrontFace.Clockwise,
                    RasterizerDiscardEnable = false,
                    LineWidth = 1,
                    PolygonMode = PolygonMode.Fill,
                    DepthBiasEnable = false,
                    DepthClampEnable = false
                },
                ColorBlendInfo =
                {
                    LogicOpEnable = false,
                    Attachments = colorBlendAttachment
                },
                DepthStencilInfo =
                {
                    DepthTestEnable = true,
                    DepthWriteEnable = true,
                    DepthCompareOp = CompareOp.LessOrEqual,
                    MinDepthBounds = 0,
                    MaxDepthBounds = 100,
                    StencilTestEnable = false,
                    DepthBoundsTestEnable = false
                }
            };
            return pipelineDescription;
        }
    }

    [RegisterGraphicsPipeline("texture")]
    internal static unsafe GraphicsPipelineDescription TextureDescription
    {
        get
        {
            Rect2D scissor = new()
            {
                Extent = VulkanEngine.SwapchainExtent,
                Offset = new Offset2D(0, 0)
            };
            Viewport viewport = new()
            {
                Width = VulkanEngine.SwapchainExtent.Width,
                Height = VulkanEngine.SwapchainExtent.Height,
                MaxDepth = 1f,
                MinDepth = 0f
            };

            var vertexInputBindings = new[]
            {
                Vertex.GetVertexBinding(),
                new VertexInputBindingDescription
                {
                    Binding = 1,
                    Stride = (uint) sizeof(Matrix4x4),
                    InputRate = VertexInputRate.Instance
                }
            };

            var attributes = Vertex.GetVertexAttributes();
            var vertexInputAttributes =
                new VertexInputAttributeDescription[attributes.Length + 4];
            for (var i = 0; i < attributes.Length; i++) vertexInputAttributes[i] = attributes[i];

            vertexInputAttributes[attributes.Length] = new VertexInputAttributeDescription
            {
                Binding = 1,
                Format = Format.R32G32B32A32Sfloat,
                Location = (uint) attributes.Length,
                Offset = 0
            };
            vertexInputAttributes[attributes.Length + 1] = new VertexInputAttributeDescription
            {
                Binding = 1,
                Format = Format.R32G32B32A32Sfloat,
                Location = (uint) attributes.Length + 1,
                Offset = (uint) sizeof(Vector4)
            };
            vertexInputAttributes[attributes.Length + 2] = new VertexInputAttributeDescription
            {
                Binding = 1,
                Format = Format.R32G32B32A32Sfloat,
                Location = (uint) attributes.Length + 2,
                Offset = (uint) sizeof(Vector4) * 2
            };
            vertexInputAttributes[attributes.Length + 3] = new VertexInputAttributeDescription
            {
                Binding = 1,
                Format = Format.R32G32B32A32Sfloat,
                Location = (uint) attributes.Length + 3,
                Offset = (uint) sizeof(Vector4) * 3
            };

            var colorBlendAttachment = new[]
            {
                new PipelineColorBlendAttachmentState
                {
                    BlendEnable = Vk.True,
                    SrcColorBlendFactor = BlendFactor.SrcAlpha,
                    DstColorBlendFactor = BlendFactor.OneMinusSrcAlpha,
                    ColorBlendOp = BlendOp.Add,
                    SrcAlphaBlendFactor = BlendFactor.One,
                    DstAlphaBlendFactor = BlendFactor.Zero,
                    AlphaBlendOp = BlendOp.Add,
                    ColorWriteMask = ColorComponentFlags.ColorComponentRBit |
                                     ColorComponentFlags.ColorComponentGBit |
                                     ColorComponentFlags.ColorComponentBBit | ColorComponentFlags.ColorComponentABit
                }
            };

            var dynamicStates = new[]
            {
                DynamicState.Viewport,
                DynamicState.Scissor
            };

            GraphicsPipelineDescription pipelineDescription = new()
            {
                Shaders = new[]
                {
                    ShaderIDs.TriangleVert,
                    ShaderIDs.TextureFrag
                },
                Scissors = new[] {scissor},
                Viewports = new[] {viewport},
                DescriptorSets = new[] {DescriptorSetIDs.CameraBuffer, DescriptorSetIDs.SampledTexture},
                Flags = 0,
                Topology = PrimitiveTopology.TriangleList,
                DynamicStates = dynamicStates,
                RenderPass = default,
                SampleCount = SampleCountFlags.SampleCount1Bit,
                SubPass = 0,
                BasePipelineHandle = default,
                BasePipelineIndex = 0,
                PrimitiveRestartEnable = false,
                AlphaToCoverageEnable = false,
                VertexAttributeDescriptions = vertexInputAttributes,
                VertexInputBindingDescriptions = vertexInputBindings,
                RasterizationInfo =
                {
                    CullMode = CullModeFlags.CullModeNone,
                    FrontFace = FrontFace.Clockwise,
                    RasterizerDiscardEnable = false,
                    LineWidth = 1,
                    PolygonMode = PolygonMode.Fill,
                    DepthBiasEnable = false,
                    DepthClampEnable = false
                },
                ColorBlendInfo =
                {
                    LogicOpEnable = false,
                    Attachments = colorBlendAttachment
                },
                DepthStencilInfo =
                {
                    DepthTestEnable = true,
                    DepthWriteEnable = true,
                    DepthCompareOp = CompareOp.LessOrEqual,
                    MinDepthBounds = 0,
                    MaxDepthBounds = 100,
                    StencilTestEnable = false,
                    DepthBoundsTestEnable = false
                }
            };
            return pipelineDescription;
        }
    }

    [RegisterGraphicsPipeline("ui_overlay")]
    internal static unsafe GraphicsPipelineDescription UiOverlayDescription
    {
        get
        {
            Rect2D scissor = new()
            {
                Extent = VulkanEngine.SwapchainExtent,
                Offset = new Offset2D(0, 0)
            };
            Viewport viewport = new()
            {
                Width = VulkanEngine.SwapchainExtent.Width,
                Height = VulkanEngine.SwapchainExtent.Height,
                MaxDepth = 1f,
                MinDepth = 0f
            };

            var vertexInputBindings = new[]
            {
                Vertex.GetVertexBinding(),
                new VertexInputBindingDescription
                {
                    Binding = 1,
                    Stride = (uint) sizeof(Matrix4x4),
                    InputRate = VertexInputRate.Instance
                }
            };

            var attributes = Vertex.GetVertexAttributes();
            var vertexInputAttributes =
                new VertexInputAttributeDescription[attributes.Length + 4];
            for (var i = 0; i < attributes.Length; i++) vertexInputAttributes[i] = attributes[i];

            vertexInputAttributes[attributes.Length] = new VertexInputAttributeDescription
            {
                Binding = 1,
                Format = Format.R32G32B32A32Sfloat,
                Location = (uint) attributes.Length,
                Offset = 0
            };
            vertexInputAttributes[attributes.Length + 1] = new VertexInputAttributeDescription
            {
                Binding = 1,
                Format = Format.R32G32B32A32Sfloat,
                Location = (uint) attributes.Length + 1,
                Offset = (uint) sizeof(Vector4)
            };
            vertexInputAttributes[attributes.Length + 2] = new VertexInputAttributeDescription
            {
                Binding = 1,
                Format = Format.R32G32B32A32Sfloat,
                Location = (uint) attributes.Length + 2,
                Offset = (uint) sizeof(Vector4) * 2
            };
            vertexInputAttributes[attributes.Length + 3] = new VertexInputAttributeDescription
            {
                Binding = 1,
                Format = Format.R32G32B32A32Sfloat,
                Location = (uint) attributes.Length + 3,
                Offset = (uint) sizeof(Vector4) * 3
            };

            var colorBlendAttachment = new[]
            {
                new PipelineColorBlendAttachmentState
                {
                    BlendEnable = Vk.True,
                    SrcColorBlendFactor = BlendFactor.SrcAlpha,
                    DstColorBlendFactor = BlendFactor.OneMinusSrcAlpha,
                    ColorBlendOp = BlendOp.Add,
                    SrcAlphaBlendFactor = BlendFactor.One,
                    DstAlphaBlendFactor = BlendFactor.Zero,
                    AlphaBlendOp = BlendOp.Add,
                    ColorWriteMask = ColorComponentFlags.ColorComponentRBit |
                                     ColorComponentFlags.ColorComponentGBit |
                                     ColorComponentFlags.ColorComponentBBit | ColorComponentFlags.ColorComponentABit
                }
            };

            var dynamicStates = new[]
            {
                DynamicState.Viewport,
                DynamicState.Scissor
            };

            GraphicsPipelineDescription pipelineDescription = new()
            {
                Shaders = new[]
                {
                    ShaderIDs.UiOverlayVert,
                    ShaderIDs.UiOverlayFrag
                },
                Scissors = new[] {scissor},
                Viewports = new[] {viewport},
                DescriptorSets = new[] {DescriptorSetIDs.SampledTexture},
                Flags = 0,
                Topology = PrimitiveTopology.TriangleList,
                DynamicStates = dynamicStates,
                RenderPass = default,
                SampleCount = SampleCountFlags.SampleCount1Bit,
                SubPass = 0,
                BasePipelineHandle = default,
                BasePipelineIndex = 0,
                PrimitiveRestartEnable = false,
                AlphaToCoverageEnable = false,
                VertexAttributeDescriptions = vertexInputAttributes,
                VertexInputBindingDescriptions = vertexInputBindings,
                RasterizationInfo =
                {
                    CullMode = CullModeFlags.CullModeNone,
                    FrontFace = FrontFace.Clockwise,
                    RasterizerDiscardEnable = false,
                    LineWidth = 1,
                    PolygonMode = PolygonMode.Fill,
                    DepthBiasEnable = false,
                    DepthClampEnable = false
                },
                ColorBlendInfo =
                {
                    LogicOpEnable = false,
                    Attachments = colorBlendAttachment
                },
                DepthStencilInfo =
                {
                    DepthTestEnable = true,
                    DepthWriteEnable = true,
                    DepthCompareOp = CompareOp.LessOrEqual,
                    MinDepthBounds = 0,
                    MaxDepthBounds = 100,
                    StencilTestEnable = false,
                    DepthBoundsTestEnable = false
                }
            };
            var uiVertInput =
                new VertexInputAttributeDescription[attributes.Length];
            for (var i = 0; i < attributes.Length; i++) uiVertInput[i] = attributes[i];
            pipelineDescription.VertexAttributeDescriptions = uiVertInput;

            var uiVertBinding = new[] {Vertex.GetVertexBinding()};
            pipelineDescription.VertexInputBindingDescriptions = uiVertBinding;
            return pipelineDescription;
        }
    }

    [RegisterShader("triangle_vert", "triangle_vert.spv")]
    internal static ShaderInfo TriangleVertShaderInfo => new(ShaderStageFlags.ShaderStageVertexBit);

    [RegisterShader("color_frag", "color_frag.spv")]
    internal static ShaderInfo ColorFragShaderInfo => new(ShaderStageFlags.ShaderStageFragmentBit);

    [RegisterShader("common_vert", "common_vert.spv")]
    internal static ShaderInfo CommonVertShaderInfo => new(ShaderStageFlags.ShaderStageVertexBit);

    [RegisterShader("wireframe_frag", "wireframe_frag.spv")]
    internal static ShaderInfo WireframeFragShaderInfo => new(ShaderStageFlags.ShaderStageFragmentBit);

    [RegisterShader("texture_frag", "texture_frag.spv")]
    internal static ShaderInfo TextureFragShaderInfo => new(ShaderStageFlags.ShaderStageFragmentBit);

    [RegisterShader("ui_overlay_vert", "ui_overlay_vert.spv")]
    internal static ShaderInfo UiOverlayVertShaderInfo => new(ShaderStageFlags.ShaderStageVertexBit);

    [RegisterShader("ui_overlay_frag", "ui_overlay_frag.spv")]
    internal static ShaderInfo UiOverlayFragShaderInfo => new(ShaderStageFlags.ShaderStageFragmentBit);

    [RegisterKeyAction("back_to_main_menu")]
    public static KeyActionInfo BackToMainMenu => new()
    {
        Key = Silk.NET.Input.Key.Escape,
        Action = delegate { Engine.ShouldStop = true; },

        KeyStatus = KeyStatus.KeyDown
    };
}