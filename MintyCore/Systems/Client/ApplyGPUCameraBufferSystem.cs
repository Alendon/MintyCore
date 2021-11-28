using System.Numerics;
using MintyCore.Components.Client;
using MintyCore.Components.Common;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Render;
using MintyCore.SystemGroups;
using MintyCore.Utils;

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
            /*if (World is null) return;
            foreach (var (buffer, resourceSet) in CameraBuffers[World])
            {
                buffer.Dispose();
                resourceSet.Dispose();
            }

            CameraBuffers.Remove(World);*/
        }

        protected override void Execute()
        {
            /*foreach (var entity in _cameraQuery)
            {
                if (World.EntityManager.GetEntityOwner(entity.Entity) != Engine.LocalPlayerGameId) continue;

                var camera = entity.GetCamera();
                var position = entity.GetPosition();

                var cameraMatrix = Matrix4x4.CreateLookAt(position.Value + camera.PositionOffset,
                    position.Value + camera.PositionOffset + camera.Forward, camera.Upward);
                var camProjection = Matrix4x4.CreatePerspectiveFieldOfView(camera.Fov,
                    (float)Engine.Window.GetWindow().Width / Engine.Window.GetWindow().Height, 0.1f, 200f);
                VulkanEngine.UpdateBuffer(CameraBuffers[World][FrameNumber[World]].buffer,
                    cameraMatrix * camProjection);
            }*/
        }

        public override void Setup()
        {
            /*_cameraQuery.Setup(this);

            CameraBuffers.Add(World, new (DeviceBuffer, ResourceSet)[FrameCount]);
            for (var i = 0; i < CameraBuffers[World].Length; i++)
            {
                var buffer = VulkanEngine.CreateBuffer<Matrix4x4>(BufferUsage.UniformBuffer);
                ResourceSetDescription resourceSetDescription =
                    new(ResourceLayoutHandler.GetResourceLayout(ResourceLayoutIDs.Camera), buffer);
                var resourceSet = VulkanEngine.ResourceFactory.CreateResourceSet(ref resourceSetDescription);

                CameraBuffers[World][i].buffer = buffer;
                CameraBuffers[World][i].resourceSet = resourceSet;
            }*/
        }
    }
}