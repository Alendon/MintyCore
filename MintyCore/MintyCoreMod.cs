using System;
using System.Numerics;
using MintyCore.Components.Client;
using MintyCore.Components.Common;
using MintyCore.Components.Common.Physic;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Network.Messages;
using MintyCore.Registries;
using MintyCore.Render;
using MintyCore.SystemGroups;
using MintyCore.Systems.Client;
using MintyCore.Systems.Common;
using MintyCore.Systems.Common.Physics;
using MintyCore.UI;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore;

/// <summary>
///     The Engine/CoreGame <see cref="IMod" /> which adds all essential stuff to the game
/// </summary>
[RootMod]
public sealed class MintyCoreMod : IMod
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
    public GameType ExecutionSide => GameType.LOCAL;

    /// <inheritdoc />
    public void PreLoad()
    {
    }

    /// <inheritdoc />
    public void Load()
    {
        RegistryIDs.Component = RegistryManager.AddRegistry<ComponentRegistry>(ModId,"component");
        RegistryIDs.System = RegistryManager.AddRegistry<SystemRegistry>(ModId,"system");
        RegistryIDs.Archetype = RegistryManager.AddRegistry<ArchetypeRegistry>(ModId,"archetype");
        RegistryIDs.World = RegistryManager.AddRegistry<WorldRegistry>("world");

        RegistryIDs.Message = RegistryManager.AddRegistry<MessageRegistry>(ModId,"message");

        RegistryIDs.Texture = RegistryManager.AddRegistry<TextureRegistry>(ModId,"texture", "textures");
        RegistryIDs.Shader = RegistryManager.AddRegistry<ShaderRegistry>(ModId,"shader", "shaders");
        RegistryIDs.Pipeline = RegistryManager.AddRegistry<PipelineRegistry>(ModId,"pipeline");
        RegistryIDs.Material = RegistryManager.AddRegistry<MaterialRegistry>(ModId,"material");
        RegistryIDs.RenderPass = RegistryManager.AddRegistry<RenderPassRegistry>(ModId,"render_pass");
        RegistryIDs.DescriptorSet = RegistryManager.AddRegistry<DescriptorSetRegistry>(ModId,"descriptor_set");

        RegistryIDs.Mesh = RegistryManager.AddRegistry<MeshRegistry>(ModId,"mesh", "models");
        RegistryIDs.InstancedRenderData =
            RegistryManager.AddRegistry<InstancedRenderDataRegistry>(ModId,"indexed_render_data");
        RegistryIDs.Font = RegistryManager.AddRegistry<FontRegistry>(ModId,"font", "fonts");
        RegistryIDs.Image = RegistryManager.AddRegistry<ImageRegistry>(ModId,"image", "images");
        RegistryIDs.Ui = RegistryManager.AddRegistry<UiRegistry>(ModId,"ui");

        ComponentRegistry.OnRegister += RegisterComponents;
        SystemRegistry.OnRegister += RegisterSystems;
        ArchetypeRegistry.OnRegister += RegisterArchetypes;
        WorldRegistry.OnRegister += RegisterWorlds;

        MessageRegistry.OnRegister += RegisterMessages;

        TextureRegistry.OnRegister += RegisterTextures;
        ShaderRegistry.OnRegister += RegisterShaders;
        PipelineRegistry.OnRegister += RegisterPipelines;
        MaterialRegistry.OnRegister += RegisterMaterials;
        DescriptorSetRegistry.OnRegister += RegisterDescriptorSets;

        MeshRegistry.OnRegister += RegisterMeshes;
        InstancedRenderDataRegistry.OnRegister += RegisterIndexedRenderData;
        FontRegistry.OnRegister += RegisterFonts;
        ImageRegistry.OnRegister += RegisterImages;
        UiRegistry.OnRegister += RegisterUi;

        //Engine.OnDrawGameUi += DrawConnectedPlayersUi;
    }

    private void RegisterWorlds()
    {
        WorldIDs.Default = WorldRegistry.RegisterWorld(ModId, "default", server => new World(server));
    }

    /// <inheritdoc />
    public void PostLoad()
    {
    }

    /// <inheritdoc />
    public void Unload()
    {
        // Engine.OnDrawGameUi -= DrawConnectedPlayersUi;
    }

    private void RegisterUi()
    {
        UiIDs.MainMenu = UiRegistry.RegisterUiRoot(ModId, "main_menu", new MainMenu());
    }

    private void RegisterImages()
    {
        ImageIDs.UiCornerUpperLeft =
            ImageRegistry.RegisterImage(ModId, "ui_corner_upper_left", "ui_corner_upper_left.png");
        ImageIDs.UiCornerUpperRight =
            ImageRegistry.RegisterImage(ModId, "ui_corner_upper_right", "ui_corner_upper_right.png");
        ImageIDs.UiCornerLowerLeft =
            ImageRegistry.RegisterImage(ModId, "ui_corner_lower_left", "ui_corner_lower_left.png");
        ImageIDs.UiCornerLowerRight =
            ImageRegistry.RegisterImage(ModId, "ui_corner_lower_right", "ui_corner_lower_right.png");


        ImageIDs.UiBorderLeft = ImageRegistry.RegisterImage(ModId, "ui_border_left", "ui_border_left.png");
        ImageIDs.UiBorderRight = ImageRegistry.RegisterImage(ModId, "ui_border_right", "ui_border_right.png");
        ImageIDs.UiBorderTop = ImageRegistry.RegisterImage(ModId, "ui_border_top", "ui_border_top.png");
        ImageIDs.UiBorderBottom = ImageRegistry.RegisterImage(ModId, "ui_border_bottom", "ui_border_bottom.png");

        ImageIDs.MainMenuBackground =
            ImageRegistry.RegisterImage(ModId, "main_menu_background", "main_menu_background.png");
    }

    private void RegisterFonts()
    {
        FontIDs.Akashi = FontRegistry.RegisterFontFamily(ModId, "akashi", "akashi.ttf");
    }

    private void RegisterArchetypes()
    {
        ArchetypeIDs.TestRender = ArchetypeRegistry.RegisterArchetype(
            new ArchetypeContainer(ComponentIDs.InstancedRenderAble, ComponentIDs.Transform, ComponentIDs.Position,
                ComponentIDs.Rotation, ComponentIDs.Scale), ModId, "test_render");
    }

    private void RegisterIndexedRenderData()
    {
        InstancedRenderDataIDs.Testing =
            InstancedRenderDataRegistry.RegisterInstancedRenderData(ModId, "testing", MeshIDs.Cube,
                MaterialIDs.Ground);
    }

    private void RegisterDescriptorSets()
    {
        DescriptorSetLayoutBinding[] bindings =
        {
            new()
            {
                Binding = 0,
                DescriptorCount = 1,
                DescriptorType = DescriptorType.UniformBuffer,
                StageFlags = ShaderStageFlags.ShaderStageVertexBit
            }
        };

        DescriptorSetIDs.CameraBuffer =
            DescriptorSetRegistry.RegisterDescriptorSet(ModId, "camera_buffer", bindings.AsSpan());

        DescriptorSetLayoutBinding[] textureBindings =
        {
            new()
            {
                Binding = 0,
                DescriptorCount = 1,
                DescriptorType = DescriptorType.CombinedImageSampler,
                StageFlags = ShaderStageFlags.ShaderStageFragmentBit
            }
        };

        DescriptorSetIDs.SampledTexture =
            DescriptorSetRegistry.RegisterDescriptorSet(ModId, "sampled_texture", textureBindings.AsSpan());
    }

    private void RegisterMaterials()
    {
        MaterialIDs.Triangle = MaterialRegistry.RegisterMaterial(ModId, "triangle", PipelineIDs.Color);

        MaterialIDs.Ground = MaterialRegistry.RegisterMaterial(ModId, "ground_texture", PipelineIDs.Texture,
            (TextureHandler.GetTextureBindResourceSet(TextureIDs.Ground), 1));

        MaterialIDs.UiOverlay = MaterialRegistry.RegisterMaterial(ModId, "ui_overlay", PipelineIDs.UiOverlay);
    }

    private void RegisterTextures()
    {
        TextureIDs.Ground = TextureRegistry.RegisterTexture(ModId, "ground", "dirt.png");
        TextureIDs.Dirt = TextureRegistry.RegisterTexture(ModId, "dirt", "dirt.png");
    }

    private unsafe void RegisterPipelines()
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

        Span<VertexInputBindingDescription> vertexInputBindings = stackalloc[]
        {
            Vertex.GetVertexBinding(),
            new VertexInputBindingDescription
            {
                Binding = 1,
                Stride = (uint)sizeof(Matrix4x4),
                InputRate = VertexInputRate.Instance
            }
        };

        var attributes = Vertex.GetVertexAttributes();
        Span<VertexInputAttributeDescription> vertexInputAttributes =
            stackalloc VertexInputAttributeDescription[attributes.Length + 4];
        for (var i = 0; i < attributes.Length; i++) vertexInputAttributes[i] = attributes[i];

        vertexInputAttributes[attributes.Length] = new VertexInputAttributeDescription
        {
            Binding = 1,
            Format = Format.R32G32B32A32Sfloat,
            Location = (uint)attributes.Length,
            Offset = 0
        };
        vertexInputAttributes[attributes.Length + 1] = new VertexInputAttributeDescription
        {
            Binding = 1,
            Format = Format.R32G32B32A32Sfloat,
            Location = (uint)attributes.Length + 1,
            Offset = (uint)sizeof(Vector4)
        };
        vertexInputAttributes[attributes.Length + 2] = new VertexInputAttributeDescription
        {
            Binding = 1,
            Format = Format.R32G32B32A32Sfloat,
            Location = (uint)attributes.Length + 2,
            Offset = (uint)sizeof(Vector4) * 2
        };
        vertexInputAttributes[attributes.Length + 3] = new VertexInputAttributeDescription
        {
            Binding = 1,
            Format = Format.R32G32B32A32Sfloat,
            Location = (uint)attributes.Length + 3,
            Offset = (uint)sizeof(Vector4) * 3
        };

        Span<PipelineColorBlendAttachmentState> colorBlendAttachment =
            stackalloc PipelineColorBlendAttachmentState[]
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

        Span<DynamicState> dynamicStates = stackalloc DynamicState[]
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
            Scissors = new ReadOnlySpan<Rect2D>(&scissor, 1),
            Viewports = new ReadOnlySpan<Viewport>(&viewport, 1),
            DescriptorSets = new[] { DescriptorSetIDs.CameraBuffer },
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

        PipelineIDs.Color = PipelineRegistry.RegisterGraphicsPipeline(ModId, "color", pipelineDescription);

        pipelineDescription.Shaders[1] = ShaderIDs.Texture;
        pipelineDescription.DescriptorSets = new[] { DescriptorSetIDs.CameraBuffer, DescriptorSetIDs.SampledTexture };
        PipelineIDs.Texture = PipelineRegistry.RegisterGraphicsPipeline(ModId, "texture", pipelineDescription);

        pipelineDescription.Shaders[0] = ShaderIDs.UiOverlayVert;
        pipelineDescription.Shaders[1] = ShaderIDs.UiOverlayFrag;
        pipelineDescription.DescriptorSets = new[] { DescriptorSetIDs.SampledTexture };

        Span<VertexInputAttributeDescription> uiVertInput =
            stackalloc VertexInputAttributeDescription[attributes.Length];
        for (var i = 0; i < attributes.Length; i++) uiVertInput[i] = attributes[i];
        pipelineDescription.VertexAttributeDescriptions = uiVertInput;

        Span<VertexInputBindingDescription> uiVertBinding = stackalloc VertexInputBindingDescription[1]
            { Vertex.GetVertexBinding() };
        pipelineDescription.VertexInputBindingDescriptions = uiVertBinding;


        PipelineIDs.UiOverlay = PipelineRegistry.RegisterGraphicsPipeline(ModId, "ui_overlay", pipelineDescription);
    }

    private void RegisterMeshes()
    {
        MeshIDs.Suzanne = MeshRegistry.RegisterMesh(ModId, "suzanne", "suzanne.obj");
        MeshIDs.Square = MeshRegistry.RegisterMesh(ModId, "square", "square.obj");
        MeshIDs.Capsule = MeshRegistry.RegisterMesh(ModId, "capsule", "capsule.obj");
        MeshIDs.Cube = MeshRegistry.RegisterMesh(ModId, "cube", "cube.obj");
        MeshIDs.Sphere = MeshRegistry.RegisterMesh(ModId, "sphere", "sphere.obj");
    }

    private void RegisterShaders()
    {
        ShaderIDs.TriangleVert = ShaderRegistry.RegisterShader(ModId, "triangle_vert", "triangle_vert.spv",
            ShaderStageFlags.ShaderStageVertexBit);
        ShaderIDs.ColorFrag =
            ShaderRegistry.RegisterShader(ModId, "color_frag", "color_frag.spv",
                ShaderStageFlags.ShaderStageFragmentBit);
        ShaderIDs.CommonVert =
            ShaderRegistry.RegisterShader(ModId, "common_vert", "common_vert.spv",
                ShaderStageFlags.ShaderStageVertexBit);
        ShaderIDs.WireframeFrag =
            ShaderRegistry.RegisterShader(ModId, "wireframe_frag", "wireframe_frag.spv",
                ShaderStageFlags.ShaderStageFragmentBit);
        ShaderIDs.Texture =
            ShaderRegistry.RegisterShader(ModId, "texture_frag", "texture_frag.spv",
                ShaderStageFlags.ShaderStageFragmentBit);
        ShaderIDs.UiOverlayVert =
            ShaderRegistry.RegisterShader(ModId, "ui_overlay_vert", "ui_overlay_vert.spv",
                ShaderStageFlags.ShaderStageVertexBit);

        ShaderIDs.UiOverlayFrag = ShaderRegistry.RegisterShader(ModId, "ui_overlay_frag", "ui_overlay_frag.spv",
            ShaderStageFlags.ShaderStageFragmentBit);
    }

    private void RegisterSystems()
    {
        SystemGroupIDs.Initialization =
            SystemRegistry.RegisterSystem<InitializationSystemGroup>(ModId, "initialization_system_group");
        SystemGroupIDs.Simulation =
            SystemRegistry.RegisterSystem<SimulationSystemGroup>(ModId, "simulation_system_group");
        SystemGroupIDs.Finalization =
            SystemRegistry.RegisterSystem<FinalizationSystemGroup>(ModId, "finalization_system_group");
        SystemGroupIDs.Presentation =
            SystemRegistry.RegisterSystem<PresentationSystemGroup>(ModId, "presentation_system_group");
        SystemGroupIDs.Physic = SystemRegistry.RegisterSystem<PhysicSystemGroup>(ModId, "physic_system_group");

        SystemIDs.ApplyTransform = SystemRegistry.RegisterSystem<ApplyTransformSystem>(ModId, "apply_transform");

        SystemIDs.ApplyGpuCameraBuffer =
            SystemRegistry.RegisterSystem<ApplyGpuCameraBufferSystem>(ModId, "apply_gpu_camera_buffer");
        SystemIDs.RenderInstanced = SystemRegistry.RegisterSystem<RenderInstancedSystem>(ModId, "render_indexed");

        SystemIDs.Collision = SystemRegistry.RegisterSystem<CollisionSystem>(ModId, "collision");
        SystemIDs.MarkCollidersDirty =
            SystemRegistry.RegisterSystem<MarkCollidersDirty>(ModId, "mark_colliders_dirty");
    }

    private void RegisterComponents()
    {
        ComponentIDs.Position = ComponentRegistry.RegisterComponent<Position>(ModId, "position");
        ComponentIDs.Rotation = ComponentRegistry.RegisterComponent<Rotation>(ModId, "rotation");
        ComponentIDs.Scale = ComponentRegistry.RegisterComponent<Scale>(ModId, "scale");
        ComponentIDs.Transform = ComponentRegistry.RegisterComponent<Transform>(ModId, "transform");
        ComponentIDs.Renderable = ComponentRegistry.RegisterComponent<RenderAble>(ModId, "renderable");
        ComponentIDs.InstancedRenderAble =
            ComponentRegistry.RegisterComponent<InstancedRenderAble>(ModId, "indexed_renderable");
        ComponentIDs.Camera = ComponentRegistry.RegisterComponent<Camera>(ModId, "camera");

        ComponentIDs.Mass = ComponentRegistry.RegisterComponent<Mass>(ModId, "mass");
        ComponentIDs.Collider = ComponentRegistry.RegisterComponent<Collider>(ModId, "collider");
    }

    private void RegisterMessages()
    {
        MessageIDs.LoadMods = MessageRegistry.RegisterMessage<LoadMods>(ModId, "load_mods");
        MessageIDs.PlayerConnected = MessageRegistry.RegisterMessage<PlayerConnected>(ModId, "player_connected");
        MessageIDs.PlayerInformation = MessageRegistry.RegisterMessage<PlayerInformation>(ModId, "player_information");
        MessageIDs.RequestPlayerInformation =
            MessageRegistry.RegisterMessage<RequestPlayerInformation>(ModId, "request_player_information");
        
        MessageIDs.AddEntity = MessageRegistry.RegisterMessage<AddEntity>(ModId, "add_entity");
        MessageIDs.RemoveEntity = MessageRegistry.RegisterMessage<RemoveEntity>(ModId, "remove_entity");
        MessageIDs.ComponentUpdate = MessageRegistry.RegisterMessage<ComponentUpdate>(ModId, "component_update");
        MessageIDs.SendEntityData = MessageRegistry.RegisterMessage<SendEntityData>(ModId, "send_entity_data");
        MessageIDs.PlayerJoined = MessageRegistry.RegisterMessage<PlayerJoined>(ModId, "player_joined");
        MessageIDs.PlayerLeft = MessageRegistry.RegisterMessage<PlayerLeft>(ModId, "player_left");
        MessageIDs.SyncPlayers = MessageRegistry.RegisterMessage<SyncPlayers>(ModId, "sync_players");
    }
}