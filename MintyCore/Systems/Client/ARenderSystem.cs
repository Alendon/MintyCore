using MintyCore.ECS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace MintyCore.Systems.Client
{
	abstract class ARenderSystem : ASystem
	{
		protected static Dictionary<World, (DeviceBuffer, ResourceSet)[]> _cameraBuffers = new();

		protected const int _frameCount = 5;

		protected static Dictionary<World, int> _frameNumber = new();
		protected static Dictionary<World, (DeviceBuffer, ResourceSet)[]> _worldBuffers = new();
		protected static Dictionary<World, Entity[]> _entityPerIndex = new();
		protected static Dictionary<World, Dictionary<Entity, int>> _entitiesPerIndex = new();
	}
}
