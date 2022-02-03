using MintyCore.Systems.Client;
using MintyCore.Utils;

namespace MintyCore.Identifications;

/// <summary>
///     <see langword="static" /> partial class which contains all <see cref="ECS.ASystem" /> ids
/// </summary>
public static class SystemIDs
{
    /// <summary>
    ///     <see cref="Identification" /> of the <see cref="Systems.Common.ApplyTransformSystem" />
    /// </summary>
    public static Identification ApplyTransform { get; internal set; }

    /// <summary>
    ///     <see cref="Identification" /> of the <see cref="ApplyGpuCameraBufferSystem" />
    /// </summary>
    public static Identification ApplyGpuCameraBuffer { get; internal set; }

    /// <summary>
    ///     <see cref="Identification" /> of the <see cref="Systems.Common.Physics.CollisionSystem" />
    /// </summary>
    public static Identification Collision { get; internal set; }

    /// <summary>
    ///     <see cref="Identification" /> of the <see cref="Systems.Common.Physics.MarkCollidersDirty" />
    /// </summary>
    public static Identification MarkCollidersDirty { get; internal set; }
        
    /// <summary>
    ///     <see cref="Identification" /> of the <see cref="Systems.Client.RenderInstancedSystem" />
    /// </summary>
    public static Identification RenderInstanced { get; internal set; }
}