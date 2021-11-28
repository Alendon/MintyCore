using System.Collections.Generic;
using MintyCore.ECS;
using MintyCore.Render;

namespace MintyCore.Systems.Client
{
	/// <summary>
	///     <see langword="abstract" /> base class for all rendering systems for sharing data
	/// </summary>
	public abstract class ARenderSystem : ASystem
    {
	    /// <summary>
	    ///     Base size for the transform buffer
	    /// </summary>
	    protected const int InitialTransformCount = 256;

	    /// <summary>
	    ///     Dictionary with CameraBuffers for each RenderWorld
	    /// </summary>
	    //protected static readonly Dictionary<World, (DeviceBuffer buffer, ResourceSet resourceSet)[]> CameraBuffers = new();

	    /// <summary>
	    ///     The current frameNumber per world
	    /// </summary>
	    protected static readonly Dictionary<World, int> FrameNumber = new();

	    /// <summary>
	    ///     Buffer to store the transforms on the gpu
	    /// </summary>
	    //protected static readonly Dictionary<World, (DeviceBuffer buffer, ResourceSet resourceSet)> TransformBuffer = new();

	    /// <summary>
	    ///     Collection to store the Entity at each index of the gpu buffer
	    /// </summary>
	    protected static readonly Dictionary<World, Entity[]> EntityPerIndex = new();

	    /// <summary>
	    ///     Collection to store the Entity Index at the gpu buffer
	    /// </summary>
	    protected static readonly Dictionary<World, Dictionary<Entity, int>> EntityIndexes = new();

	    /// <summary>
	    ///     Number to specify the frame data overlap
	    /// </summary>
	    protected int FrameCount => VulkanEngine._swapchainImages.Length;
    }
}