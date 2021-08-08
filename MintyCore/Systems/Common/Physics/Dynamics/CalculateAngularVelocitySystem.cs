using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MintyCore.Components.Common.Physic.Dynamics;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.SystemGroups;
using MintyCore.Utils;

namespace MintyCore.Systems.Common.Physics.Dynamics
{
	[ExecuteInSystemGroup(typeof(PhysicSystemGroup))]
	[ExecuteAfter(typeof(CalculateAngularAcclerationSystem))]
	public partial class CalculateAngularVelocitySystem : ASystem
	{
		[ComponentQuery]
		AcclerationDampingVelocityQuery<(AngularVelocity, AngularAccleration), AngularDamping> _query = new();

		public override Identification Identification => SystemIDs.CalculateAngularVelocity;

		public override void Dispose()
		{

		}

		public override void Execute()
		{
			foreach (var entity in _query)
			{
				ref AngularVelocity velocity = ref entity.GetAngularVelocity();
				ref AngularAccleration accleration = ref entity.GetAngularAccleration();
				AngularDamping damping = entity.GetAngularDamping();

				velocity.Value += accleration.Value * MintyCore.FixedDeltaTime;
				velocity.Value *= MathF.Pow(damping.Value, MintyCore.FixedDeltaTime);
				accleration.Value = System.Numerics.Vector3.Zero;
			}
		}

		public override void Setup()
		{
			_query.Setup(this);
		}
	}
}
