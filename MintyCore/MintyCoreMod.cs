using System;
using System.Numerics;
using ImGuiNET;
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
using MintyCore.Utils;
using Silk.NET.Vulkan;
using SixLabors.ImageSharp.PixelFormats;

namespace MintyCore;

/// <summary>
///     The Engine/CoreGame <see cref="IMod" /> which adds all essential stuff to the game
/// </summary>
[RootMod]
public class MintyCoreMod : IMod
{
    /// <summary>
    ///     The Instance of the <see cref="MintyCoreMod" />
    /// </summary>
    public static MintyCoreMod? Instance;

    /// <summary/>
    public MintyCoreMod()
    {
        Instance = this;
    }

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
        RegistryIDs.Component = RegistryManager.AddRegistry<ComponentRegistry>("component");
        RegistryIDs.System = RegistryManager.AddRegistry<SystemRegistry>("system");
        RegistryIDs.Archetype = RegistryManager.AddRegistry<ArchetypeRegistry>("archetype");

        RegistryIDs.Message = RegistryManager.AddRegistry<MessageRegistry>("message");

        RegistryIDs.Texture = RegistryManager.AddRegistry<TextureRegistry>("texture", "textures");
        RegistryIDs.Shader = RegistryManager.AddRegistry<ShaderRegistry>("shader", "shaders");
        RegistryIDs.Pipeline = RegistryManager.AddRegistry<PipelineRegistry>("pipeline");
        RegistryIDs.Material = RegistryManager.AddRegistry<MaterialRegistry>("material");
        RegistryIDs.RenderPass = RegistryManager.AddRegistry<RenderPassRegistry>("render_pass");
        RegistryIDs.DescriptorSet = RegistryManager.AddRegistry<DescriptorSetRegistry>("descriptor_set");

        RegistryIDs.Mesh = RegistryManager.AddRegistry<MeshRegistry>("mesh", "models");
        RegistryIDs.InstancedRenderData =
            RegistryManager.AddRegistry<InstancedRenderDataRegistry>("indexed_render_data");

        ComponentRegistry.OnRegister += RegisterComponents;
        SystemRegistry.OnRegister += RegisterSystems;
        ArchetypeRegistry.OnRegister += RegisterArchetypes;

        MessageRegistry.OnRegister += RegisterMessages;

        TextureRegistry.OnRegister += RegisterTextures;
        ShaderRegistry.OnRegister += RegisterShaders;
        PipelineRegistry.OnRegister += RegisterPipelines;
        MaterialRegistry.OnRegister += RegisterMaterials;
        RenderPassRegistry.OnRegister += RegisterRenderPass;
        DescriptorSetRegistry.OnRegister += RegisterDescriptorSets;

        MeshRegistry.OnRegister += RegisterMeshes;
        InstancedRenderDataRegistry.OnRegister += RegisterIndexedRenderData;

        //Engine.OnDrawGameUi += DrawConnectedPlayersUi;
    }

    /// <inheritdoc />
    public void PostLoad()
    {
    }

    /// <inheritdoc/>
    public void Unload()
    {
        // Engine.OnDrawGameUi -= DrawConnectedPlayersUi;
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
                StageFlags = ShaderStageFlags.ShaderStageVertexBit,
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

    private void RegisterRenderPass()
    {
    }

    private void RegisterMaterials()
    {
        MaterialIDs.Triangle = MaterialRegistry.RegisterMaterial(ModId, "triangle", PipelineIDs.Color);

        MaterialIDs.Ground = MaterialRegistry.RegisterMaterial(ModId, "ground_texture", PipelineIDs.Texture,
            (TextureHandler.GetTextureBindResourceSet(TextureIDs.Ground), 1));

        MaterialIDs.UiOverlay = MaterialRegistry.RegisterMaterial(ModId, "ui_overlay", PipelineIDs.UiOverlay);
    }

    private unsafe void RegisterTextures()
    {
        TextureIDs.Ground = TextureRegistry.RegisterTexture(ModId, "ground", "dirt.png");
        TextureIDs.Dirt = TextureRegistry.RegisterTexture(ModId, "dirt", "dirt.png");

            
        TextureIDs.UiCornerUpperLeft = TextureRegistry.RegisterTexture(ModId,  "ui_corner_upper_left", "ui_corner_upper_left.png", false, cpuOnly: true, flipY:true);
        TextureIDs.UiCornerUpperRight = TextureRegistry.RegisterTexture(ModId, "ui_corner_upper_right", "ui_corner_upper_right.png", false, cpuOnly: true, flipY:true);
        TextureIDs.UiCornerLowerLeft = TextureRegistry.RegisterTexture(ModId,  "ui_corner_lower_left", "ui_corner_lower_left.png", false, cpuOnly: true, flipY:true);
        TextureIDs.UiCornerLowerRight = TextureRegistry.RegisterTexture(ModId, "ui_corner_lower_right", "ui_corner_lower_right.png", false, cpuOnly: true, flipY:true);

            
        TextureIDs.UiBorderLeft = TextureRegistry.RegisterTexture(ModId, "ui_border_left", "ui_border_left.png", false, cpuOnly: true, flipY:true);
        TextureIDs.UiBorderRight = TextureRegistry.RegisterTexture(ModId, "ui_border_right", "ui_border_right.png", false, cpuOnly: true, flipY:true);
        TextureIDs.UiBorderTop = TextureRegistry.RegisterTexture(ModId, "ui_border_top", "ui_border_top.png", false, cpuOnly: true, flipY:true);
        TextureIDs.UiBorderBottom = TextureRegistry.RegisterTexture(ModId, "ui_border_bottom", "ui_border_bottom.png", false, cpuOnly: true, flipY:true);
            
            
            

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
            shaders = new[]
            {
                ShaderIDs.TriangleVert,
                ShaderIDs.ColorFrag
            },
            scissors = new ReadOnlySpan<Rect2D>(&scissor, 1),
            viewports = new ReadOnlySpan<Viewport>(&viewport, 1),
            descriptorSets = new[] { DescriptorSetIDs.CameraBuffer },
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
            vertexAttributeDescriptions = vertexInputAttributes,
            vertexINputBindingDescriptions = vertexInputBindings,
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

        pipelineDescription.shaders[1] = ShaderIDs.Texture;
        pipelineDescription.descriptorSets = new[] { DescriptorSetIDs.CameraBuffer, DescriptorSetIDs.SampledTexture };
        PipelineIDs.Texture = PipelineRegistry.RegisterGraphicsPipeline(ModId, "texture", pipelineDescription);

        pipelineDescription.shaders[0] = ShaderIDs.UiOverlayVert;
        pipelineDescription.shaders[1] = ShaderIDs.UiOverlayFrag;
        pipelineDescription.descriptorSets = new[] { DescriptorSetIDs.SampledTexture };
            
        Span<VertexInputAttributeDescription> uiVertInput = stackalloc VertexInputAttributeDescription[attributes.Length];
        for (int i = 0; i < attributes.Length; i++) uiVertInput[i] = attributes[i];
        pipelineDescription.vertexAttributeDescriptions = uiVertInput;

        Span<VertexInputBindingDescription> uiVertBinding = stackalloc VertexInputBindingDescription[1]
            { Vertex.GetVertexBinding() };
        pipelineDescription.vertexINputBindingDescriptions = uiVertBinding;
            
            
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
        SystemIDs.DrawUiOverlay = SystemRegistry.RegisterSystem<DrawUiOverlay>(ModId, "draw_ui_overlay");

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
        MessageIDs.AddEntity = MessageRegistry.RegisterMessage<AddEntity>(ModId, "add_entity");
        MessageIDs.RemoveEntity = MessageRegistry.RegisterMessage<RemoveEntity>(ModId, "remove_entity");
        MessageIDs.ComponentUpdate = MessageRegistry.RegisterMessage<ComponentUpdate>(ModId, "component_update");
        MessageIDs.SendEntityData = MessageRegistry.RegisterMessage<SendEntityData>(ModId, "send_entity_data");
        MessageIDs.PlayerJoined = MessageRegistry.RegisterMessage<PlayerJoined>(ModId, "player_joined");
        MessageIDs.PlayerLeft = MessageRegistry.RegisterMessage<PlayerLeft>(ModId, "player_left");
        MessageIDs.SyncPlayers = MessageRegistry.RegisterMessage<SyncPlayers>(ModId, "sync_players");
    }

    private static void DrawConnectedPlayersUi()
    {
        ImGui.Begin("Connected Players");

        ImGui.BeginChild("");
        foreach (var gameId in Engine.GetConnectedPlayers())
            ImGui.Text(
                $"{Engine.GetPlayerName(gameId)}; GameID: '{gameId}'; PlayerID: '{Engine.GetPlayerId(gameId)}'");

        ImGui.EndChild();

        ImGui.End();
    }
}