using System.Numerics;
using MintyCore.Components.Client;
using MintyCore.Components.Common;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Render;
using MintyCore.SystemGroups;
using MintyCore.Utils;
using MintyVeldrid;

namespace MintyCore.Systems.Client
{
    [ExecuteInSystemGroup(typeof(PresentationSystemGroup))]
    [ExecuteAfter(typeof(IncreaseFrameNumberSystem))]
    internal partial class ApplyGpuCameraBufferSystem : ARenderSystem
    {
        [ComponentQuery] private readonly Query<object, (Camera, Position)> _cameraQuery = new();

        public override Identification Identification => SystemIDs.ApplyGpuCameraBuffer;


        public override void Dispose()
        {
            if (World is null) return;
            foreach (var (buffer, resourceSet) in CameraBuffers[World])
            {
                buffer.Dispose();
                resourceSet.Dispose();
            }

            CameraBuffers.Remove(World);
        }

        protected override void Execute()
        {
            foreach (var entity in _cameraQuery)
            {
                if (World.EntityManager.GetEntityOwner(entity.Entity) != MintyCore.LocalPlayerGameId) continue;

                var camera = entity.GetCamera();
                var position = entity.GetPosition();

                var cameraPosition = position.Value + camera.PositionOffset;

                var cameraMatrix = Matrix4x4.CreateLookAt(position.Value + camera.PositionOffset, cameraPosition +
                    Matrix4x4.Transform(Matrix4x4.CreateTranslation(new Vector3(0, 0, -1)), camera.Rotation)
                        .Translation,
                    new Vector3(0, 1, 0));
                var camProjection = Matrix4x4.CreatePerspectiveFieldOfView(camera.Fov,
                    (float)MintyCore.Window.GetWindow().Width / MintyCore.Window.GetWindow().Height, 0.1f, 200f);
                VulkanEngine.UpdateBuffer(CameraBuffers[World][FrameNumber[World]].buffer,
                    cameraMatrix * camProjection);
            }
        }

        public override void Setup()
        {
            _cameraQuery.Setup(this);

            CameraBuffers.Add(World, new (DeviceBuffer, ResourceSet)[FrameCount]);
            for (var i = 0; i < CameraBuffers[World].Length; i++)
            {
                var buffer = VulkanEngine.CreateBuffer<Matrix4x4>(BufferUsage.UniformBuffer);
                ResourceSetDescription resourceSetDescription =
                    new(ResourceLayoutHandler.GetResourceLayout(ResourceLayoutIDs.Camera), buffer);
                var resourceSet = VulkanEngine.ResourceFactory.CreateResourceSet(ref resourceSetDescription);

                CameraBuffers[World][i].buffer = buffer;
                CameraBuffers[World][i].resourceSet = resourceSet;
            }
        }
    }
}