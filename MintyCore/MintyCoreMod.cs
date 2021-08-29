using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

using MintyCore.Components;
using MintyCore.Components.Client;
using MintyCore.Components.Common;
using MintyCore.Components.Common.Physic.Collisions;
using MintyCore.Components.Common.Physic.Dynamics;
using MintyCore.Components.Common.Physic.Forces;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Registries;
using MintyCore.Render;
using MintyCore.SystemGroups;
using MintyCore.Systems;
using MintyCore.Systems.Client;
using MintyCore.Systems.Common;
using MintyCore.Systems.Common.Physics;
using MintyCore.Utils;

using Veldrid;
using Vector3 = BulletSharp.Math.Vector3;

namespace MintyCore
{
	/// <summary>
	/// The Engine/CoreGame <see cref="IMod"/> which adds all essential stuff to the game
	/// </summary>
	public class MintyCoreMod : IMod
	{
		/// <summary>
		/// The Instance of thhe <see cref="MintyCoreMod"/>
		/// </summary>
		public static MintyCoreMod Instance;

		internal MintyCoreMod()
		{
			Instance = this;
		}

		/// <inheritdoc/>
		public ushort ModID { get; private set; }

		private readonly DeletionQueue _deletionQueue = new DeletionQueue();

		/// <inheritdoc/>
		public void Dispose()
		{
			_deletionQueue.Flush();
		}

		/// <inheritdoc/>
		public string StringIdentifier => "techardry_core";

		/// <inheritdoc/>
		public void Register(ushort modID)
		{
			ModID = modID;

			RegistryIDs.Component = RegistryManager.AddRegistry<ComponentRegistry>("component");
			RegistryIDs.System = RegistryManager.AddRegistry<SystemRegistry>("system");
			RegistryIDs.Archetype = RegistryManager.AddRegistry<ArchetypeRegistry>("archetype");

			RegistryIDs.Texture = RegistryManager.AddRegistry<TextureRegistry>("texture", "textures");


			RegistryIDs.Shader = RegistryManager.AddRegistry<ShaderRegistry>("shader", "shaders");
			RegistryIDs.Pipeline = RegistryManager.AddRegistry<PipelineRegistry>("pipeline");
			RegistryIDs.Material = RegistryManager.AddRegistry<MaterialRegistry>("material");
			RegistryIDs.MaterialCollection =
				RegistryManager.AddRegistry<MaterialCollectionRegistry>("material_collection");
			RegistryIDs.ResourceLayout = RegistryManager.AddRegistry<ResourceLayoutRegistry>("resource_layout");

			RegistryIDs.Mesh = RegistryManager.AddRegistry<MeshRegistry>("mesh", "models");

			ComponentRegistry.OnRegister += RegisterComponents;
			SystemRegistry.OnRegister += RegisterSystems;
			ArchetypeRegistry.OnRegister += RegisterArchetypes;

			TextureRegistry.OnRegister += RegisterTextures;

			ShaderRegistry.OnRegister += RegisterShaders;
			PipelineRegistry.OnRegister += RegisterPipelines;
			MaterialRegistry.OnRegister += RegisterMaterials;
			MaterialCollectionRegistry.OnRegister += RegisterMaterialCollections;
			ResourceLayoutRegistry.OnRegister += RegisterResourceLayouts;

			MeshRegistry.OnRegister += RegisterMeshes;
		}

		private void RegisterResourceLayouts()
		{
			ResourceLayoutElementDescription cameraResourceLayoutElementDescription = new("camera_buffer", ResourceKind.UniformBuffer, ShaderStages.Vertex);
			ResourceLayoutDescription cameraResourceLayoutDescription = new(cameraResourceLayoutElementDescription);
			ResourceLayoutIDs.Camera = ResourceLayoutRegistry.RegisterResourceLayout(ModID, "camera_buffer", ref cameraResourceLayoutDescription);

			ResourceLayoutElementDescription transformResourceLayoutElementDescription = new("transform_buffer", ResourceKind.StructuredBufferReadOnly, ShaderStages.Vertex);
			ResourceLayoutDescription transformResourceLayoutDescription = new(transformResourceLayoutElementDescription);
			ResourceLayoutIDs.Transform = ResourceLayoutRegistry.RegisterResourceLayout(ModID, "transform_buffer", ref transformResourceLayoutDescription);

			ResourceLayoutElementDescription samplerResourceLayoutElementDescription = new("sampler", ResourceKind.Sampler, ShaderStages.Fragment);
			ResourceLayoutElementDescription textureResourceLayoutElementDescription = new("texture", ResourceKind.TextureReadOnly, ShaderStages.Fragment);
			ResourceLayoutDescription samplerResourceLayoutDescription = new(samplerResourceLayoutElementDescription, textureResourceLayoutElementDescription);
			ResourceLayoutIDs.Sampler = ResourceLayoutRegistry.RegisterResourceLayout(ModID, "sampler", ref samplerResourceLayoutDescription);
		}

		private void RegisterMaterialCollections()
		{
			MaterialCollectionIDs.BasicColorCollection =
				MaterialCollectionRegistry.RegisterMaterialCollection(ModID, "basic_color_collection",
					MaterialIDs.Color);
			MaterialCollectionIDs.GroundTexture = MaterialCollectionRegistry.RegisterMaterialCollection(ModID, "ground_texture", MaterialIDs.Ground);
		}

		private void RegisterMaterials()
		{
			MaterialIDs.Color =
				MaterialRegistry.RegisterMaterial(ModID, "color", PipelineHandler.GetPipeline(PipelineIDs.Color));
			MaterialIDs.Ground = MaterialRegistry.RegisterMaterial(ModID, "ground_texture",
				PipelineHandler.GetPipeline(PipelineIDs.Texture),
				(TextureHandler.GetTextureBindResourceSet(TextureIDs.Ground), 2));
		}

		private void RegisterTextures()
		{
			TextureIDs.Ground = TextureRegistry.RegisterTexture(ModID, "gound", "dirt.png");
		}

		private void RegisterPipelines()
		{
			GraphicsPipelineDescription pipelineDescription = new()
			{
				BlendState = BlendStateDescription.SingleOverrideBlend,
				DepthStencilState =
				new DepthStencilStateDescription(true, true, ComparisonKind.LessEqual),
				RasterizerState = new RasterizerStateDescription(FaceCullMode.None,
				PolygonFillMode.Solid, FrontFace.Clockwise, true, true),


				ResourceLayouts = new ResourceLayout[]
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

			PipelineIDs.Color = PipelineRegistry.RegisterGraphicsPipeline(ModID, "color", ref pipelineDescription);

			pipelineDescription.RasterizerState.FillMode = PolygonFillMode.Wireframe;
			pipelineDescription.ShaderSet.Shaders[0] = ShaderHandler.GetShader(ShaderIDs.WireframeFrag);
			PipelineIDs.WireFrame = PipelineRegistry.RegisterGraphicsPipeline(ModID, "wireframe", ref pipelineDescription);

			pipelineDescription.RasterizerState.FillMode = PolygonFillMode.Solid;
			pipelineDescription.ShaderSet.Shaders[0] = ShaderHandler.GetShader(ShaderIDs.Texture);
			pipelineDescription.ResourceLayouts = new ResourceLayout[]
			{
				ResourceLayoutHandler.GetResourceLayout(ResourceLayoutIDs.Camera),
				ResourceLayoutHandler.GetResourceLayout(ResourceLayoutIDs.Transform),
				ResourceLayoutHandler.GetResourceLayout(ResourceLayoutIDs.Sampler)
			};
			PipelineIDs.Texture = PipelineRegistry.RegisterGraphicsPipeline(ModID, "texture", ref pipelineDescription);
		}

		private void RegisterMeshes()
		{
			MeshIDs.Suzanne = MeshRegistry.RegisterMesh(ModID, "suzanne", "suzanne.obj");
			MeshIDs.Square = MeshRegistry.RegisterMesh(ModID, "square", "square.obj");
			MeshIDs.Capsule = MeshRegistry.RegisterMesh(ModID, "capsule", "capsule.obj");
			MeshIDs.Cube = MeshRegistry.RegisterMesh(ModID, "cube", "cube.obj");
			MeshIDs.Sphere = MeshRegistry.RegisterMesh(ModID, "sphere", "sphere.obj");
		}

		private void RegisterShaders()
		{
			ShaderIDs.ColorFrag =
				ShaderRegistry.RegisterShader(ModID, "color_frag", "color_frag.spv", ShaderStages.Fragment);
			ShaderIDs.CommonVert = ShaderRegistry.RegisterShader(ModID, "common_vert", "common_vert.spv", ShaderStages.Vertex);
			ShaderIDs.WireframeFrag = ShaderRegistry.RegisterShader(ModID, "wireframe_frag", "wireframe_frag.spv", ShaderStages.Fragment);
			ShaderIDs.Texture = ShaderRegistry.RegisterShader(ModID, "texture_frag", "texture_frag.spv", ShaderStages.Fragment);
		}

		void RegisterSystems()
		{
			SystemGroupIDs.Initialization =
				SystemRegistry.RegisterSystem<InitializationSystemGroup>(ModID, "initialization_system_group");
			SystemGroupIDs.Simulation = SystemRegistry.RegisterSystem<SimulationSystemGroup>(ModID, "simulation_system_group");
			SystemGroupIDs.Finalization = SystemRegistry.RegisterSystem<FinalizationSystemGroup>(ModID, "finalization_system_group");
			SystemGroupIDs.Presentation = SystemRegistry.RegisterSystem<PresentationSystemGroup>(ModID, "presentation_system_group");
			SystemGroupIDs.Physic = SystemRegistry.RegisterSystem<PhysicSystemGroup>(ModID, "physic_system_group");

			SystemIDs.ApplyTransform = SystemRegistry.RegisterSystem<ApplyTransformSystem>(ModID, "apply_transform");

			SystemIDs.IncreaseFrameNumber = SystemRegistry.RegisterSystem<IncreaseFrameNumberSystem>(ModID, "increase_frame_number");
			SystemIDs.ApplyGPUCameraBuffer = SystemRegistry.RegisterSystem<ApplyGPUCameraBufferSystem>(ModID, "apply_gpu_camera_buffer");
			SystemIDs.ApplyGPUTransformBuffer = SystemRegistry.RegisterSystem<ApplyGPUTransformBufferSystem>(ModID, "apply_gpu_transform_buffer");
			SystemIDs.RenderMesh = SystemRegistry.RegisterSystem<RenderMeshSystem>(ModID, "render_mesh");
			SystemIDs.RenderWireFrame = SystemRegistry.RegisterSystem<RenderWireFrameSystem>(ModID, "render_wireframe");

			SystemIDs.Movement = SystemRegistry.RegisterSystem<MovementSystem>(ModID, "movement");

			SystemIDs.Input = SystemRegistry.RegisterSystem<InputSystem>(ModID, "input");

			SystemIDs.Rotator = SystemRegistry.RegisterSystem<RotatorTestSystem>(ModID, "rotator");

			//SystemIDs.CalculateAngularAccleration = SystemRegistry.RegisterSystem<CalculateAngularAcclerationSystem>(ModID, "calculate_angular_accleration");
			//SystemIDs.CalculateAngularVelocity = SystemRegistry.RegisterSystem<CalculateAngularVelocitySystem>(ModID, "calculate_angular_velocity");
			//SystemIDs.CalculateRotation = SystemRegistry.RegisterSystem<CalculateRotationSystem>(ModID, "calculate_rotation");

			//SystemIDs.CalculateLinearAccleration = SystemRegistry.RegisterSystem<CalculateLinearAcclerationSystem>(ModID, "calculate_linear_accleration");
			//SystemIDs.CalculateLinearVelocity = SystemRegistry.RegisterSystem<CalculateLinearVelocitySystem>(ModID, "calculate_linear_velocity");
			//SystemIDs.CalculatePosition = SystemRegistry.RegisterSystem<CalculatePositionSystem>(ModID, "calculate_position");

			//SystemIDs.GravityGenerator = SystemRegistry.RegisterSystem<GravityGeneratorSystem>(ModID, "gravity_generator");
			//SystemIDs.SpringGenerator = SystemRegistry.RegisterSystem<SpringGeneratorSystem>(ModID, "spring_generator");
			SystemIDs.Collision = SystemRegistry.RegisterSystem<CollisionSystem>(ModID, "collision");
		}

		void RegisterComponents()
		{
			ComponentIDs.Position = ComponentRegistry.RegisterComponent<Position>(ModID, "position");
			ComponentIDs.Rotation = ComponentRegistry.RegisterComponent<Rotation>(ModID, "rotation");
			ComponentIDs.Scale = ComponentRegistry.RegisterComponent<Scale>(ModID, "scale");
			ComponentIDs.Transform = ComponentRegistry.RegisterComponent<Transform>(ModID, "transform");
			ComponentIDs.Renderable = ComponentRegistry.RegisterComponent<Renderable>(ModID, "renderable");
			ComponentIDs.Input = ComponentRegistry.RegisterComponent<Input>(ModID, "input");
			ComponentIDs.Camera = ComponentRegistry.RegisterComponent<Camera>(ModID, "camera");
			ComponentIDs.Rotator = ComponentRegistry.RegisterComponent<Rotator>(ModID, "rotator");

			ComponentIDs.Mass = ComponentRegistry.RegisterComponent<Mass>(ModID, "mass");
			ComponentIDs.LinearDamping = ComponentRegistry.RegisterComponent<LinearDamping>(ModID, "linear_damping");
			ComponentIDs.Force = ComponentRegistry.RegisterComponent<Force>(ModID, "force");
			ComponentIDs.Accleration = ComponentRegistry.RegisterComponent<Accleration>(ModID, "accleration");
			ComponentIDs.Velocity = ComponentRegistry.RegisterComponent<Velocity>(ModID, "velocity");

			ComponentIDs.Inertia = ComponentRegistry.RegisterComponent<Inertia>(ModID, "inertia");
			ComponentIDs.AngularDamping = ComponentRegistry.RegisterComponent<AngularDamping>(ModID, "angular_damping");
			ComponentIDs.Torgue = ComponentRegistry.RegisterComponent<Torque>(ModID, "torgue");
			ComponentIDs.AngularAccleration = ComponentRegistry.RegisterComponent<AngularAccleration>(ModID, "angular_accleration");
			ComponentIDs.AngularVelocity = ComponentRegistry.RegisterComponent<AngularVelocity>(ModID, "angular_velocity");

			ComponentIDs.Gravity = ComponentRegistry.RegisterComponent<Gravity>(ModID, "gravity");
			ComponentIDs.Spring = ComponentRegistry.RegisterComponent<Spring>(ModID, "spring");
			ComponentIDs.Collider = ComponentRegistry.RegisterComponent<Collider>(ModID, "collider");
		}

		void RegisterArchetypes()
		{
			ArchetypeContainer player = new ArchetypeContainer(new HashSet<Utils.Identification>()
			{
				ComponentIDs.Rotation,
				ComponentIDs.Position,
				ComponentIDs.Scale,
				ComponentIDs.Transform,
				ComponentIDs.Camera,
				ComponentIDs.Input
			});

			ArchetypeContainer mesh = new ArchetypeContainer(new HashSet<Utils.Identification>()
			{
				ComponentIDs.Rotation,
				ComponentIDs.Position,
				ComponentIDs.Scale,
				ComponentIDs.Transform,
				ComponentIDs.Renderable,
				ComponentIDs.Rotator
			});

			ArchetypeContainer rigidBody = new ArchetypeContainer(new HashSet<Identification>()
			{
				ComponentIDs.Rotation,
				ComponentIDs.Position,
				ComponentIDs.Scale,
				ComponentIDs.Transform,

				ComponentIDs.Renderable,

				ComponentIDs.Mass,
				ComponentIDs.LinearDamping,
				ComponentIDs.Force,
				ComponentIDs.Accleration,
				ComponentIDs.Velocity,

				ComponentIDs.Inertia,
				ComponentIDs.AngularDamping,
				ComponentIDs.Torgue,
				ComponentIDs.AngularAccleration,
				ComponentIDs.AngularVelocity,

				ComponentIDs.Gravity,
				ComponentIDs.Collider
				
			});

			ArchetypeIDs.Player = ArchetypeRegistry.RegisterArchetype(player, ModID, "player");
			ArchetypeIDs.Mesh = ArchetypeRegistry.RegisterArchetype(mesh, ModID, "mesh");
			ArchetypeIDs.RigidBody = ArchetypeRegistry.RegisterArchetype(rigidBody, ModID, "rigid_body");
		}
	}
}