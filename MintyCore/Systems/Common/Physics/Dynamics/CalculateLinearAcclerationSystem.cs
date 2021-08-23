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
	[ExecuteBefore(typeof(CalculateLinearVelocitySystem))]
	public partial class CalculateLinearAcclerationSystem : ASystem
	{
		[ComponentQuery]
		ForceMassAcclerationQuery<(Accleration, Force), Mass> _query = new();

		public override Identification Identification => SystemIDs.CalculateLinearAccleration;

		public override void Dispose()
		{
		}

		public override void Execute()
		{
			foreach (var entity in _query)
			{
				ref Accleration accleration = ref entity.GetAccleration();
				ref Force force = ref entity.GetForce();
				Mass mass = entity.GetMass();

				accleration.Value = force.Value * mass.InverseMass;

				//Set the force value to zero for the next tick
				force.Value = System.Numerics.Vector3.Zero;
			}
		}

		public override void Setup()
		{
			_query.Setup(this);
		}
	}
}
