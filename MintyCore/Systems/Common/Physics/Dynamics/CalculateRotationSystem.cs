using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

using MintyCore.Components.Common;
using MintyCore.Components.Common.Physic.Dynamics;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.SystemGroups;
using MintyCore.Utils;
using MintyCore.Utils.Maths;

namespace MintyCore.Systems.Common.Physics.Dynamics
{
	[ExecuteInSystemGroup(typeof(PhysicSystemGroup))]
	[ExecuteAfter(typeof(CalculateAngularVelocitySystem))]
	public partial class CalculateRotationSystem : ASystem
	{
		[ComponentQuery]
		VelocityRotationQuery<Rotation, AngularVelocity> _query = new();

		public override Identification Identification => SystemIDs.CalculateRotation;

		public override void Dispose()
		{
		}

		public override void Execute()
		{
			foreach (var entity in _query)
			{
				ref Rotation rotation = ref entity.GetRotation();
				AngularVelocity velocity = entity.GetAngularVelocity();

				rotation.Value = Quaternion.Normalize( rotation.Value.RotateByScaledVector(velocity.Value, MintyCore.FixedDeltaTime));
			}
		}

		public override void Setup()
		{
			_query.Setup(this);
		}
	}
}
