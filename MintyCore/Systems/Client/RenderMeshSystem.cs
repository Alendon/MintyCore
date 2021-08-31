using MintyCore.Components.Client;
using MintyCore.Components.Common;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Render;
using MintyCore.SystemGroups;
using MintyCore.Utils;
using Veldrid;

namespace MintyCore.Systems.Client
{
	/// <summary>
	///     System to render meshes
	/// </summary>
	[ExecuteInSystemGroup(typeof(PresentationSystemGroup))]
    [ExecutionSide(GameType.CLIENT)]
    [ExecuteAfter(typeof(ApplyGpuCameraBufferSystem), typeof(ApplyGpuTransformBufferSystem))]
    public partial class RenderMeshSystem : ARenderSystem
    {
        private (CommandList cl, bool rebuild)[]? _commandLists;

        [ComponentQuery] private readonly ComponentQuery<object, (RenderAble, Transform)> _renderableQuery = new();

        /// <inheritdoc />
        public override Identification Identification => SystemIDs.RenderMesh;

        /// <inheritdoc />
        public override void Setup()
        {
            _renderableQuery.Setup(this);

            VulkanEngine.OnWindowResize += OnWindowResize;

            _commandLists = new (CommandList cl, bool rebuild)[FrameCount];

            for (var i = 0; i < _commandLists.Length; i++) _commandLists[i] = (null, true);

            EntityManager.PostEntityCreateEvent += (_, _) =>
            {
                for (var i = 0; i < _commandLists.Length; i++) _commandLists[i].rebuild = true;
            };
            EntityManager.PreEntityDeleteEvent += (_, _) =>
            {
                for (var i = 0; i < _commandLists.Length; i++) _commandLists[i].rebuild = true;
            };
        }

        private void OnWindowResize(int width, int height)
        {
            for (var i = 0; i < _commandLists.Length; i++)
            {
                var (commandList, _) = _commandLists[i];
                _commandLists[i] = (commandList, true);
            }
        }

        /// <inheritdoc />
        public override void PreExecuteMainThread()
        {
            if (!MintyCore.RenderMode.HasFlag(MintyCore.RenderModeEnum.NORMAL)) return;
            if (_commandLists is null) return;

            (var cl, var rebuild) = _commandLists[MintyCore.Tick % FrameCount];
            if (!rebuild) return;

            cl?.FreeSecondaryCommandList();
            cl = VulkanEngine.DrawCommandList.GetSecondaryCommandList();

            cl.Begin();
            cl.SetFramebuffer(VulkanEngine.GraphicsDevice.SwapchainFramebuffer);
            _commandLists[MintyCore.Tick % FrameCount] = (cl, rebuild);
        }

        /// <inheritdoc />
        public override void PostExecuteMainThread()
        {
            if (!MintyCore.RenderMode.HasFlag(MintyCore.RenderModeEnum.NORMAL)) return;
            if (_commandLists is null) return;

            (var cl, var rebuild) = _commandLists[MintyCore.Tick % FrameCount];

            if (rebuild)
                cl.End();

            VulkanEngine.DrawCommandList.ExecuteSecondaryCommandList(cl);
            rebuild = false;
            _commandLists[MintyCore.Tick % FrameCount] = (cl, rebuild);
        }

        /// <inheritdoc />
        protected override void Execute()
        {
            if (!MintyCore.RenderMode.HasFlag(MintyCore.RenderModeEnum.NORMAL)) return;
            if (_commandLists is null) return;

            (var cl, var rebuild) = _commandLists[MintyCore.Tick % FrameCount];
            if (!rebuild) return;

            Material? lastMaterial = null;
            Mesh? lastMesh = null;

            var entityIndexes = EntityIndexes[World];

            foreach (var entity in _renderableQuery)
            {
                var renderAble = entity.GetRenderAble();

                var mesh = renderAble.GetMesh();
                if (mesh is null)
                {
                    Logger.WriteLog($"Mesh for entity {entity} is null", LogImportance.WARNING, "Rendering");
                    continue;
                }
                
                var material = renderAble.GetMaterialCollection();

                if (lastMaterial != material[0])
                {
                    material[0].BindMaterial(cl);
                    lastMaterial = material[0];
                    cl.SetGraphicsResourceSet(0, CameraBuffers[World][FrameNumber[World]].resourceSet);
                    cl.SetGraphicsResourceSet(1, TransformBuffer[World].Item2);
                }

                if (mesh != lastMesh)
                    mesh.BindMesh(cl);

                mesh.DrawMesh(cl, 0, (uint)entityIndexes[entity.Entity]);

                lastMesh = mesh;
            }

            _commandLists[MintyCore.Tick % FrameCount] = (cl, rebuild);
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            VulkanEngine.OnWindowResize -= OnWindowResize;
        }
    }
}