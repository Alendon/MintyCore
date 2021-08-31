using MintyCore.Systems.Client;
using MintyCore.Utils;

namespace MintyCore.Identifications
{
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
	    ///     <see cref="Identification" /> of the <see cref="Systems.Common.MovementSystem" />
	    /// </summary>
	    public static Identification Movement { get; internal set; }


	    /// <summary>
	    ///     <see cref="Identification" /> of the <see cref="Systems.Client.RenderMeshSystem" />
	    /// </summary>
	    public static Identification RenderMesh { get; internal set; }

	    /// <summary>
	    ///     <see cref="Identification" /> of the <see cref="Systems.Client.InputSystem" />
	    /// </summary>
	    public static Identification Input { get; internal set; }

	    /// <summary>
	    ///     <see cref="Identification" /> of the <see cref="Systems.Client.RenderWireFrameSystem" />
	    /// </summary>
	    public static Identification RenderWireFrame { get; internal set; }

	    /// <summary>
	    ///     <see cref="Identification" /> of the <see cref="Systems.Client.IncreaseFrameNumberSystem" />
	    /// </summary>
	    public static Identification IncreaseFrameNumber { get; internal set; }

	    /// <summary>
	    ///     <see cref="Identification" /> of the <see cref="ApplyGpuTransformBufferSystem" />
	    /// </summary>
	    public static Identification ApplyGpuTransformBuffer { get; internal set; }

	    /// <summary>
	    ///     <see cref="Identification" /> of the <see cref="ApplyGpuCameraBufferSystem" />
	    /// </summary>
	    public static Identification ApplyGpuCameraBuffer { get; internal set; }

	    /// <summary>
	    ///     <see cref="Identification" /> of the <see cref="Systems.Common.Physics.CollisionSystem" />
	    /// </summary>
	    public static Identification Collision { get; internal set; }
    }
}