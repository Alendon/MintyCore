using System.Collections.Generic;
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
using MintyCore.Utils;
using Veldrid;

namespace MintyCore
{
	/// <summary>
	///     The Engine/CoreGame <see cref="IMod" /> which adds all essential stuff to the game
	/// </summary>
	public class MintyCoreMod : IMod
    {
	    /// <summary>
	    ///     The Instance of thhe <see cref="MintyCoreMod" />
	    /// </summary>
	    public static MintyCoreMod? Instance;

        private readonly DeletionQueue _deletionQueue = new();

        internal MintyCoreMod()
        {
            Instance = this;
        }

        /// <inheritdoc />
        public ushort ModId { get; private set; }

        /// <inheritdoc />
        public void Dispose()
        {
            _deletionQueue.Flush();
        }

        /// <inheritdoc />
        public string StringIdentifier => "techardry_core";

        /// <inheritdoc />
        public void Load(ushort modId)
        {
            ModId = modId;

            RegistryIDs.Component = RegistryManager.AddRegistry<ComponentRegistry>("component");
            RegistryIDs.System = RegistryManager.AddRegistry<SystemRegistry>("system");
            RegistryIDs.Archetype = RegistryManager.AddRegistry<ArchetypeRegistry>("archetype");

            RegistryIDs.Message = RegistryManager.AddRegistry<MessageRegistry>("message");

            RegistryIDs.Texture = RegistryManager.AddRegistry<TextureRegistry>("texture", "textures");
            RegistryIDs.Shader = RegistryManager.AddRegistry<ShaderRegistry>("shader", "shaders");
            RegistryIDs.Pipeline = RegistryManager.AddRegistry<PipelineRegistry>("pipeline");
            RegistryIDs.Material = RegistryManager.AddRegistry<MaterialRegistry>("material");
            RegistryIDs.ResourceLayout = RegistryManager.AddRegistry<ResourceLayoutRegistry>("resource_layout");

            RegistryIDs.Mesh = RegistryManager.AddRegistry<MeshRegistry>("mesh", "models");

            ComponentRegistry.OnRegister += RegisterComponents;
            SystemRegistry.OnRegister += RegisterSystems;
            ArchetypeRegistry.OnRegister += RegisterArchetypes;

            MessageRegistry.OnRegister += RegisterMessages;

            TextureRegistry.OnRegister += RegisterTextures;
            ShaderRegistry.OnRegister += RegisterShaders;
            PipelineRegistry.OnRegister += RegisterPipelines;
            MaterialRegistry.OnRegister += RegisterMaterials;
            ResourceLayoutRegistry.OnRegister += RegisterResourceLayouts;

            MeshRegistry.OnRegister += RegisterMeshes;
            
        }
        
        /// <inheritdoc/>
        public void Unload()
        {
            
        }

        private void RegisterResourceLayouts()
        {
            ResourceLayoutElementDescription cameraResourceLayoutElementDescription =
                new("camera_buffer", ResourceKind.UniformBuffer, ShaderStages.Vertex);
            ResourceLayoutDescription cameraResourceLayoutDescription = new(cameraResourceLayoutElementDescription);
            ResourceLayoutIDs.Camera =
                ResourceLayoutRegistry.RegisterResourceLayout(ModId, "camera_buffer",
                    ref cameraResourceLayoutDescription);

            ResourceLayoutElementDescription transformResourceLayoutElementDescription = new("transform_buffer",
                ResourceKind.StructuredBufferReadOnly, ShaderStages.Vertex);
            ResourceLayoutDescription transformResourceLayoutDescription =
                new(transformResourceLayoutElementDescription);
            ResourceLayoutIDs.Transform = ResourceLayoutRegistry.RegisterResourceLayout(ModId, "transform_buffer",
                ref transformResourceLayoutDescription);

            ResourceLayoutElementDescription samplerResourceLayoutElementDescription =
                new("sampler", ResourceKind.Sampler, ShaderStages.Fragment);
            ResourceLayoutElementDescription textureResourceLayoutElementDescription =
                new("texture", ResourceKind.TextureReadOnly, ShaderStages.Fragment);
            ResourceLayoutDescription samplerResourceLayoutDescription = new(samplerResourceLayoutElementDescription,
                textureResourceLayoutElementDescription);
            ResourceLayoutIDs.Sampler =
                ResourceLayoutRegistry.RegisterResourceLayout(ModId, "sampler", ref samplerResourceLayoutDescription);
        }

        private void RegisterMaterials()
        {
            MaterialIDs.Color =
                MaterialRegistry.RegisterMaterial(ModId, "color", PipelineHandler.GetPipeline(PipelineIDs.Color));
            MaterialIDs.Ground = MaterialRegistry.RegisterMaterial(ModId, "ground_texture",
                PipelineHandler.GetPipeline(PipelineIDs.Texture),
                (TextureHandler.GetTextureBindResourceSet(TextureIDs.Ground), 2));
        }

        private void RegisterTextures()
        {
            TextureIDs.Ground = TextureRegistry.RegisterTexture(ModId, "gound", "dirt.png");
        }

        private void RegisterPipelines()
        {
            if(VulkanEngine.GraphicsDevice is null) return;
            
            GraphicsPipelineDescription pipelineDescription = new()
            {
                BlendState = BlendStateDescription.SingleOverrideBlend,
                DepthStencilState =
                    new DepthStencilStateDescription(true, true, ComparisonKind.LessEqual),
                RasterizerState = new RasterizerStateDescription(FaceCullMode.Back,
                    PolygonFillMode.Solid, FrontFace.CounterClockwise, true, true),


                ResourceLayouts = new[]
                {
                    ResourceLayoutHandler.GetResourceLayout(ResourceLayoutIDs.Camera),
                    ResourceLayoutHandler.GetResourceLayout(ResourceLayoutIDs.Transform)
                },
                PrimitiveTopology = PrimitiveTopology.TriangleList,
                PushConstantDescriptions = new PushConstantDescription[1]
            };
            pipelineDescription.PushConstantDescriptions[0].CreateDescription<Matrix4x4>(ShaderStages.Vertex);

            pipelineDescription.ShaderSet = new ShaderSetDescription(
                new[] { new DefaultVertex().GetVertexLayout() },
                new[] { ShaderHandler.GetShader(ShaderIDs.ColorFrag), ShaderHandler.GetShader(ShaderIDs.CommonVert) });

            pipelineDescription.Outputs = VulkanEngine.GraphicsDevice.SwapchainFramebuffer.OutputDescription;

            PipelineIDs.Color = PipelineRegistry.RegisterGraphicsPipeline(ModId, "color", ref pipelineDescription);

            pipelineDescription.RasterizerState.FillMode = PolygonFillMode.Wireframe;
            pipelineDescription.RasterizerState.CullMode = FaceCullMode.None;
            
            pipelineDescription.ShaderSet.Shaders[0] = ShaderHandler.GetShader(ShaderIDs.WireframeFrag);
            PipelineIDs.WireFrame =
                PipelineRegistry.RegisterGraphicsPipeline(ModId, "wireframe", ref pipelineDescription);

            pipelineDescription.RasterizerState.FillMode = PolygonFillMode.Solid;
            pipelineDescription.ShaderSet.Shaders[0] = ShaderHandler.GetShader(ShaderIDs.Texture);
            pipelineDescription.ResourceLayouts = new[]
            {
                ResourceLayoutHandler.GetResourceLayout(ResourceLayoutIDs.Camera),
                ResourceLayoutHandler.GetResourceLayout(ResourceLayoutIDs.Transform),
                ResourceLayoutHandler.GetResourceLayout(ResourceLayoutIDs.Sampler)
            };
            PipelineIDs.Texture = PipelineRegistry.RegisterGraphicsPipeline(ModId, "texture", ref pipelineDescription);
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
            ShaderIDs.ColorFrag =
                ShaderRegistry.RegisterShader(ModId, "color_frag", "color_frag.spv", ShaderStages.Fragment);
            ShaderIDs.CommonVert =
                ShaderRegistry.RegisterShader(ModId, "common_vert", "common_vert.spv", ShaderStages.Vertex);
            ShaderIDs.WireframeFrag =
                ShaderRegistry.RegisterShader(ModId, "wireframe_frag", "wireframe_frag.spv", ShaderStages.Fragment);
            ShaderIDs.Texture =
                ShaderRegistry.RegisterShader(ModId, "texture_frag", "texture_frag.spv", ShaderStages.Fragment);
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

            SystemIDs.IncreaseFrameNumber =
                SystemRegistry.RegisterSystem<IncreaseFrameNumberSystem>(ModId, "increase_frame_number");
            SystemIDs.ApplyGpuCameraBuffer =
                SystemRegistry.RegisterSystem<ApplyGpuCameraBufferSystem>(ModId, "apply_gpu_camera_buffer");
            SystemIDs.ApplyGpuTransformBuffer =
                SystemRegistry.RegisterSystem<ApplyGpuTransformBufferSystem>(ModId, "apply_gpu_transform_buffer");
            SystemIDs.RenderMesh = SystemRegistry.RegisterSystem<RenderMeshSystem>(ModId, "render_mesh");
            SystemIDs.RenderWireFrame = SystemRegistry.RegisterSystem<RenderWireFrameSystem>(ModId, "render_wireframe");

            SystemIDs.Movement = SystemRegistry.RegisterSystem<MovementSystem>(ModId, "movement");

            SystemIDs.Input = SystemRegistry.RegisterSystem<InputSystem>(ModId, "input");
            
            SystemIDs.Collision = SystemRegistry.RegisterSystem<CollisionSystem>(ModId, "collision");
        }

        private void RegisterComponents()
        {
            ComponentIDs.Position = ComponentRegistry.RegisterComponent<Position>(ModId, "position");
            ComponentIDs.Rotation = ComponentRegistry.RegisterComponent<Rotation>(ModId, "rotation");
            ComponentIDs.Scale = ComponentRegistry.RegisterComponent<Scale>(ModId, "scale");
            ComponentIDs.Transform = ComponentRegistry.RegisterComponent<Transform>(ModId, "transform");
            ComponentIDs.Renderable = ComponentRegistry.RegisterComponent<RenderAble>(ModId, "renderable");
            ComponentIDs.Input = ComponentRegistry.RegisterComponent<Input>(ModId, "input");
            ComponentIDs.Camera = ComponentRegistry.RegisterComponent<Camera>(ModId, "camera");

            ComponentIDs.Mass = ComponentRegistry.RegisterComponent<Mass>(ModId, "mass");
            ComponentIDs.Collider = ComponentRegistry.RegisterComponent<Collider>(ModId, "collider");
        }

        private void RegisterArchetypes()
        {
            var player = new ArchetypeContainer(new HashSet<Identification>
            {
                ComponentIDs.Rotation,
                ComponentIDs.Position,
                ComponentIDs.Scale,
                ComponentIDs.Transform,
                
                ComponentIDs.Renderable,
                ComponentIDs.Camera,
                ComponentIDs.Input
            });

            var mesh = new ArchetypeContainer(new HashSet<Identification>
            {
                ComponentIDs.Rotation,
                ComponentIDs.Position,
                ComponentIDs.Scale,
                ComponentIDs.Transform,
                ComponentIDs.Renderable
            });

            var rigidBody = new ArchetypeContainer(new HashSet<Identification>
            {
                ComponentIDs.Rotation,
                ComponentIDs.Position,
                ComponentIDs.Scale,
                ComponentIDs.Transform,

                ComponentIDs.Renderable,

                ComponentIDs.Mass,
                ComponentIDs.Collider
            });

            ArchetypeIDs.Player = ArchetypeRegistry.RegisterArchetype(player, ModId, "player");
            ArchetypeIDs.Mesh = ArchetypeRegistry.RegisterArchetype(mesh, ModId, "mesh");
            ArchetypeIDs.RigidBody = ArchetypeRegistry.RegisterArchetype(rigidBody, ModId, "rigid_body");
        }

        private void RegisterMessages()
        {
            MessageIDs.AddEntity = MessageRegistry.RegisterMessage<AddEntity>(ModId, "add_entity");
            MessageIDs.RemoveEntity = MessageRegistry.RegisterMessage<RemoveEntity>(ModId, "remove_entity");
            MessageIDs.ComponentUpdate = MessageRegistry.RegisterMessage<ComponentUpdate>(ModId, "component_update");
            MessageIDs.SendEntityData = MessageRegistry.RegisterMessage<SendEntityData>(ModId, "send_entity_data");
        }
    }
}