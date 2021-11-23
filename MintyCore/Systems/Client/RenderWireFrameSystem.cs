using MintyCore.Components.Client;
using MintyCore.Components.Common;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Render;
using MintyCore.SystemGroups;
using MintyCore.Utils;
using MintyCore.Utils.Maths;

namespace MintyCore.Systems.Client
{
    [ExecuteInSystemGroup(typeof(PresentationSystemGroup))]
    [ExecuteAfter(typeof(ApplyGpuCameraBufferSystem), typeof(ApplyGpuTransformBufferSystem))]
    [ExecutionSide(GameType.CLIENT)]
    internal partial class RenderWireFrameSystem : ARenderSystem
    {
        private (CommandList cl, bool rebuild)[]? _commandLists;

        [ComponentQuery] private readonly ComponentQuery<object, (RenderAble, Transform)> _renderableQuery = new();

        public override Identification Identification => SystemIDs.RenderWireFrame;

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

        public override void PreExecuteMainThread()
        {
            if (!MathHelper.IsBitSet((int)Engine.RenderMode, (int)Engine.RenderModeEnum.WIREFRAME)) return;
            if (_commandLists is null || VulkanEngine.DrawCommandList is null ||
                VulkanEngine.GraphicsDevice is null) return;

            var (cl, rebuild) = _commandLists[Engine.Tick % FrameCount];

            if (!rebuild) return;
            cl?.FreeSecondaryCommandList();
            cl = VulkanEngine.DrawCommandList.GetSecondaryCommandList();

            cl.Begin();
            cl.SetFramebuffer(VulkanEngine.GraphicsDevice.SwapchainFramebuffer);
            _commandLists[Engine.Tick % FrameCount] = (cl, true);
        }

        public override void PostExecuteMainThread()
        {
            if (!MathHelper.IsBitSet((int)Engine.RenderMode, (int)Engine.RenderModeEnum.WIREFRAME)) return;
            var (cl, rebuild) = _commandLists[Engine.Tick % FrameCount];

            if (rebuild)
                cl.End();
            VulkanEngine.DrawCommandList.ExecuteSecondaryCommandList(cl);
            rebuild = false;
            _commandLists[Engine.Tick % FrameCount] = (cl, rebuild);
        }

        protected override void Execute()
        {
            if (!MathHelper.IsBitSet((int)Engine.RenderMode, (int)Engine.RenderModeEnum.WIREFRAME)) return;
            if (_commandLists is null || World is null) return;

            var (cl, rebuild) = _commandLists[Engine.Tick % FrameCount];
            if (!rebuild) return;

            Mesh? lastMesh = null;
            cl.SetPipeline(PipelineHandler.GetPipeline(PipelineIDs.WireFrame));
            cl.SetGraphicsResourceSet(0, CameraBuffers[World][FrameNumber[World]].resourceSet);
            cl.SetGraphicsResourceSet(1, TransformBuffer[World].Item2);

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

                if (mesh != lastMesh)
                    mesh.BindMesh(cl);

                mesh.DrawMesh(cl, 0, (uint)entityIndexes[entity.Entity]);
                lastMesh = mesh;
            }

            _commandLists[Engine.Tick % FrameCount] = (cl, rebuild);
        }

        public override void Dispose()
        {
            VulkanEngine.OnWindowResize -= OnWindowResize;
        }
    }
}