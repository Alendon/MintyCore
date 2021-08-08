using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MintyCore.Components.Common;
using MintyCore.Components.Common.Physic.Dynamics;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.SystemGroups;
using MintyCore.Utils;

namespace MintyCore.Systems.Common.Physics.Dynamics
{
	[ExecuteInSystemGroup(typeof(PhysicSystemGroup))]
	[ExecuteAfter(typeof(CalculateLinearVelocitySystem))]
	public partial class CalculatePositionSystem : ASystem
	{
		[ComponentQuery]
		VelocityPositionQuery<Position, Velocity> _query = new();

		public override Identification Identification => SystemIDs.CalculatePosition;

		public override void Dispose()
		{

		}

		public override void Execute()
		{
			foreach (var entity in _query)
			{
				ref Position position = ref entity.GetPosition();
				Velocity velocity = entity.GetVelocity();

				position.Value += velocity.Value * MintyCore.FixedDeltaTime;
			}
		}

		public override void Setup()
		{
			_query.Setup(this);
		}
	}
}
