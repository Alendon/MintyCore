using MintyCore.Components.Client;
using MintyCore.Components.Common;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Render;
using MintyCore.SystemGroups;
using MintyCore.Utils;
using Silk.NET.Vulkan;

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
        private (CommandBuffer cl, bool rebuild)[]? _commandLists;

        private (bool rebuild, int frame) _forceRebuild = (false, 0);

        [ComponentQuery] private readonly ComponentQuery<object, (RenderAble, Transform)> _renderableQuery = new();

        /// <inheritdoc />
        public override Identification Identification => SystemIDs.RenderMesh;

        /// <inheritdoc />
        public override void Setup()
        {
            _renderableQuery.Setup(this);


            _commandLists = new (CommandBuffer cl, bool rebuild)[FrameCount];

            for (var i = 0; i < _commandLists.Length; i++) _commandLists[i] = (default, true);

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
            /*if (!Engine.RenderMode.HasFlag(Engine.RenderModeEnum.NORMAL)) return;
            if (_commandLists is null) return;

            

            var (cl, rebuild) = _commandLists[Engine.Tick % FrameCount];
            
            if (_forceRebuild.rebuild)
            {
                if (_forceRebuild.frame >= (Engine.Tick + FrameCount) % Engine.MaxTickCount)
                {
                    _forceRebuild.rebuild = false;
                }
                rebuild = true;
            }
            
            if (!rebuild) return;

            cl?.FreeSecondaryCommandList();
            cl = VulkanEngine.DrawCommandBuffer.GetSecondaryCommandList();

            cl.Begin();
            cl.SetFramebuffer(VulkanEngine.GraphicsDevice.SwapchainFramebuffer);
            _commandLists[Engine.Tick % FrameCount] = (cl, rebuild);*/
        }

        /// <inheritdoc />
        public override void PostExecuteMainThread()
        {
            /*if (!Engine.RenderMode.HasFlag(Engine.RenderModeEnum.NORMAL)) return;
            if (_commandLists is null) return;

            var (cl, rebuild) = _commandLists[Engine.Tick % FrameCount];

            if (rebuild) cl.End();

            VulkanEngine.DrawCommandBuffer.ExecuteSecondaryCommandList(cl);
            rebuild = false;
            _commandLists[Engine.Tick % FrameCount] = (cl, rebuild);*/
        }

        /// <inheritdoc />
        protected override void Execute()
        {
           /*if (!Engine.RenderMode.HasFlag(Engine.RenderModeEnum.NORMAL)) return;
            if (_commandLists is null) return;

            var (cl, rebuild) = _commandLists[Engine.Tick % FrameCount];
            if (!rebuild) return;

            Material? lastMaterial = null;
            Mesh? lastMesh = null;

            var entityIndexes = EntityIndexes[World];

            foreach (var entity in _renderableQuery)
            {
                if(World.EntityManager.GetEntityOwner(entity.Entity) == Engine.LocalPlayerGameId) continue;
                
                var renderAble = entity.GetRenderAble();

                var mesh = renderAble.GetMesh();
                if (mesh is null)
                {
                    Logger.WriteLog($"Mesh for entity {entity} is null", LogImportance.WARNING, "Rendering");
                    _forceRebuild = (true, Engine.Tick);
                    continue;
                }
                if (mesh != lastMesh)
                    mesh.BindMesh(cl);
                if(mesh.SubMeshIndexes is null) continue;

                for (var index = 0; index < mesh.SubMeshIndexes.Length; index++)
                {
                    var material = renderAble.GetMaterialAtIndex(index);
                    if (material is null) continue;
                        
                    if (lastMaterial != material)
                    {
                        material.BindMaterial(cl);
                        lastMaterial = material;
                        cl.SetGraphicsResourceSet(0, CameraBuffers[World][FrameNumber[World]].resourceSet);
                        cl.SetGraphicsResourceSet(1, TransformBuffer[World].Item2);
                    }
                    
                    mesh.DrawMesh(cl, (uint)index, (uint)entityIndexes[entity.Entity]);
                }

                lastMesh = mesh;
            }

            _commandLists[Engine.Tick % FrameCount] = (cl, true);*/
        }

        /// <inheritdoc />
        public override void Dispose()
        {
        }
    }
}