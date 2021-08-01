using Ara3D;
using MintyCore.ECS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace MintyCore.Systems.Client
{
	/// <summary>
	/// <see langword="abstract"/> base class for all rendering systems for sharing data
	/// </summary>
	public abstract class ARenderSystem : ASystem
	{
		/// <summary>
		/// Dictionary with CameraBuffers for each RenderWorld
		/// </summary>
		protected static Dictionary<World, (DeviceBuffer buffer, ResourceSet resourceSet)[]> _cameraBuffers = new();

		/// <summary>
		/// Number to specify the frame data overlap
		/// </summary>
		protected const int _frameCount = 5;

		/// <summary>
		/// The current frameNumber per world
		/// </summary>
		protected static Dictionary<World, int> _frameNumber = new();

		/// <summary>
		/// Base size for the transform buffer
		/// </summary>
		protected const int _initialTransformCount = 256;

		/// <summary>
		/// Buffer to store the transforms on the gpu
		/// </summary>
		protected static Dictionary<World, (DeviceBuffer buffer, ResourceSet resourceSet)> _transformBuffer = new();

		/// <summary>
		/// Collection to store the Entity at each index of the gpu buffer
		/// </summary>
		protected static Dictionary<World, Entity[]> _entityPerIndex = new();

		/// <summary>
		/// Collection to store the Entity Index at the gpu buffer
		/// </summary>
		protected static Dictionary<World, Dictionary<Entity, int>> _entityIndexes = new();
	}
}
