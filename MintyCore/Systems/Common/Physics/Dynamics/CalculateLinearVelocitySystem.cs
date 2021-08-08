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
	[ExecuteAfter(typeof(CalculateLinearAcclerationSystem))]
	public partial class CalculateLinearVelocitySystem : ASystem
	{
		[ComponentQuery]
		AcclerationDampingVelocityQuery<Velocity, (Accleration, LinearDamping)> _query = new();

		public override Identification Identification => SystemIDs.CalculateLinearVelocity;

		public override void Dispose()
		{

		}

		public override void Execute()
		{
			foreach (var entity in _query)
			{
				ref Velocity velocity = ref entity.GetVelocity();
				Accleration accleration = entity.GetAccleration();
				LinearDamping damping = entity.GetLinearDamping();

				velocity.Value += accleration.Value * MintyCore.FixedDeltaTime;
				velocity.Value *= MathF.Pow(damping.Value, MintyCore.FixedDeltaTime);
			}
		}

		public override void Setup()
		{
			_query.Setup(this);
		}
	}
}
