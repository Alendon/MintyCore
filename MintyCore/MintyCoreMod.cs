using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ara3D;
using MintyCore.Components;
using MintyCore.Components.Client;
using MintyCore.Components.Common;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Registries;
using MintyCore.Render;
using MintyCore.SystemGroups;
using MintyCore.Systems;
using MintyCore.Systems.Client;
using MintyCore.Systems.Common;
using MintyCore.Utils;
using Veldrid;

namespace MintyCore
{
	public class MintyCoreMod : IMod
	{
		public static MintyCoreMod Instance;

		public MintyCoreMod()
		{
			Instance = this;
		}

		public ushort ModID { get; private set; }

		private readonly DeletionQueue _deletionQueue = new DeletionQueue();

		public void Dispose()
		{
			_deletionQueue.Flush();
		}

		public string StringIdentifier => "techardry_core";

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

			RegistryIDs.Mesh = RegistryManager.AddRegistry<MeshRegistry>("mesh", "models");

			ComponentRegistry.OnRegister += RegisterComponents;
			SystemRegistry.OnRegister += RegisterSystems;
			ArchetypeRegistry.OnRegister += RegisterArchetypes;

			TextureRegistry.OnRegister += RegisterTextures;

			ShaderRegistry.OnRegister += RegisterShaders;
			PipelineRegistry.OnRegister += RegisterPipelines;
			MaterialRegistry.OnRegister += RegisterMaterials;
			MaterialCollectionRegistry.OnRegister += RegisterMaterialCollections;

			MeshRegistry.OnRegister += RegisterMeshes;
		}

		private void RegisterMaterialCollections()
		{
			MaterialCollectionIDs.BasicColorCollection =
				MaterialCollectionRegistry.RegisterMaterialCollection(ModID, "basic_color_collection",
					MaterialIDs.Color);
		}

		private void RegisterMaterials()
		{
			MaterialIDs.Color =
				MaterialRegistry.RegisterMaterial(ModID, "color", PipelineHandler.GetPipeline(PipelineIDs.Color));
		}

		private void RegisterTextures()
		{

		}

		public static PushConstantDescription.PushConstant<Matrix4x4> MeshMatrixPushConstant;
		public static ResourceLayout CameraResourceLayout;

		private void RegisterPipelines()
		{
			GraphicsPipelineDescription pipelineDescription = new GraphicsPipelineDescription();
			pipelineDescription.BlendState = BlendStateDescription.SingleOverrideBlend;
			pipelineDescription.DepthStencilState =
				new DepthStencilStateDescription(true, true, ComparisonKind.LessEqual);
			pipelineDescription.RasterizerState = new RasterizerStateDescription(FaceCullMode.None,
				PolygonFillMode.Solid, FrontFace.Clockwise, true, true);

			ResourceLayoutElementDescription resourceLayoutElementDescription = new("camera_buffer", ResourceKind.UniformBuffer, ShaderStages.Vertex);
			ResourceLayoutDescription resourceLayoutDescription = new(resourceLayoutElementDescription);
			CameraResourceLayout = VulkanEngine.GraphicsDevice.ResourceFactory.CreateResourceLayout(ref resourceLayoutDescription);

			pipelineDescription.ResourceLayouts = new ResourceLayout[] { CameraResourceLayout };
			pipelineDescription.PrimitiveTopology = PrimitiveTopology.TriangleList;
			pipelineDescription.PushConstantDescriptions = new PushConstantDescription[1];
			pipelineDescription.PushConstantDescriptions[0].CreateDescription<Matrix4x4>(ShaderStages.Vertex);

			MeshMatrixPushConstant = pipelineDescription.PushConstantDescriptions[0].GetPushConstant<Matrix4x4>();

			pipelineDescription.ShaderSet = new ShaderSetDescription(
				new[] { new DefaultVertex().GetVertexLayout() },
				new[] { ShaderHandler.GetShader(ShaderIDs.ColorFrag), ShaderHandler.GetShader(ShaderIDs.CommonVert) });

			pipelineDescription.Outputs = VulkanEngine.GraphicsDevice.SwapchainFramebuffer.OutputDescription;

			PipelineIDs.Color = PipelineRegistry.RegisterGraphicsPipeline(ModID, "color", ref pipelineDescription);

			pipelineDescription.RasterizerState.FillMode = PolygonFillMode.Wireframe;
			pipelineDescription.ShaderSet.Shaders[0] = ShaderHandler.GetShader(ShaderIDs.WireframeFrag); 
			PipelineIDs.WireFrame = PipelineRegistry.RegisterGraphicsPipeline(ModID, "wireframe", ref pipelineDescription);
		}

		private void RegisterMeshes()
		{
			MeshIDs.Suzanne = MeshRegistry.RegisterMesh(ModID, "suzanne", "suzanne.obj");
			MeshIDs.Square = MeshRegistry.RegisterMesh(ModID, "square", "square.obj");
		}

		private void RegisterShaders()
		{
			ShaderIDs.ColorFrag =
				ShaderRegistry.RegisterShader(ModID, "color_frag", "color_frag.spv", ShaderStages.Fragment);
			ShaderIDs.CommonVert = ShaderRegistry.RegisterShader(ModID, "common_vert", "common_vert.spv", ShaderStages.Vertex);
			ShaderIDs.WireframeFrag = ShaderRegistry.RegisterShader(ModID, "wireframe_frag", "wireframe_frag.spv", ShaderStages.Fragment);
		}

		void RegisterSystems()
		{
			SystemGroupIDs.Initialization =
				SystemRegistry.RegisterSystem<InitializationSystemGroup>(ModID, "initialization");
			SystemGroupIDs.Simulation = SystemRegistry.RegisterSystem<SimulationSystemGroup>(ModID, "simulation");
			SystemGroupIDs.Finalization = SystemRegistry.RegisterSystem<FinalizationSystemGroup>(ModID, "finalization");
			SystemGroupIDs.Presentation = SystemRegistry.RegisterSystem<PresentationSystemGroup>(ModID, "presentation");

			SystemIDs.ApplyTransform = SystemRegistry.RegisterSystem<ApplyTransformSystem>(ModID, "apply_transform");

			SystemIDs.RenderMesh = SystemRegistry.RegisterSystem<RenderMeshSystem>(ModID, "render_mesh");
			SystemIDs.RenderWireFrame = SystemRegistry.RegisterSystem<RenderWireFrameSystem>(ModID, "render_wireframe");

			SystemIDs.Movement = SystemRegistry.RegisterSystem<MovementSystem>(ModID, "movement");

			SystemIDs.Input = SystemRegistry.RegisterSystem<InputSystem>(ModID, "input");

			SystemIDs.Rotator = SystemRegistry.RegisterSystem<RotatorTestSystem>(ModID, "rotator");
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

			ArchetypeIDs.Player = ArchetypeRegistry.RegisterArchetype(player, ModID, "player");
			ArchetypeIDs.Mesh = ArchetypeRegistry.RegisterArchetype(mesh, ModID, "mesh");
		}
	}
}