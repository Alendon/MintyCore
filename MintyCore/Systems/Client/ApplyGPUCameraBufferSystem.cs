using System.Numerics;
using MintyCore.Components.Client;
using MintyCore.Components.Common;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Registries;
using MintyCore.Render;
using MintyCore.SystemGroups;
using MintyCore.Utils;

namespace MintyCore.Systems.Client;

/// <summary>
/// System which calculates the camera matrix and sends it to the graphics card.
/// </summary>
[ExecuteInSystemGroup(typeof(PresentationSystemGroup))]
[ExecutionSide(GameType.Client)]
[RegisterSystem("apply_gpu_camera_buffer")]
public partial class ApplyGpuCameraBufferSystem : ASystem
{
    [ComponentQuery] private readonly Query<Camera, Position> _cameraQuery = new();

    ///<inheritdoc/>
    public override Identification Identification => SystemIDs.ApplyGpuCameraBuffer;

    ///<inheritdoc/>
    protected override unsafe void Execute()
    {
        if (World is null) return;

        foreach (var entity in _cameraQuery)
        {
            var owner = World.EntityManager.GetEntityOwner(entity.Entity);
            if (owner != PlayerHandler.LocalPlayerGameId && owner != Constants.ServerId) continue;

            ref var camera = ref entity.GetCamera();
            var position = entity.GetPosition();

            var cameraMatrix = Matrix4x4.CreateLookAt(position.Value + camera.PositionOffset,
                position.Value + camera.PositionOffset + camera.Forward, camera.Upward);
            var camProjection = Matrix4x4.CreatePerspectiveFieldOfView(camera.Fov,
                (float) VulkanEngine.SwapchainExtent.Width / VulkanEngine.SwapchainExtent.Height, camera.NearPlane, camera.FarPlane);

            var memoryBuffer = camera.GpuTransformBuffers[(int) VulkanEngine.ImageIndex];
            var matPtr = (Matrix4x4*) MemoryManager.Map(memoryBuffer.Memory);

            *matPtr = cameraMatrix * camProjection;
            MemoryManager.UnMap(memoryBuffer.Memory);
        }
    }

    ///<inheritdoc/>
    public override void Setup(SystemManager systemManager)
    {
        _cameraQuery.Setup(this);
    }
}