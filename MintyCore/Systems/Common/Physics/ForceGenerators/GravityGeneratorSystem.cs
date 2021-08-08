using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MintyCore.Components.Common.Physic.Dynamics;
using MintyCore.Components.Common.Physic.Forces;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.SystemGroups;
using MintyCore.Systems.Common.Physics.Dynamics;
using MintyCore.Utils;

namespace MintyCore.Systems.Common.Physics.ForceGenerators
{
	[ExecuteInSystemGroup(typeof(PhysicSystemGroup))]
	[ExecuteBefore(typeof(CalculateLinearAcclerationSystem))]
	public partial class GravityGeneratorSystem : ASystem
	{
		[ComponentQuery]
		GravityQuery<Force, (Mass, Gravity)> _query = new();

		public override Identification Identification => SystemIDs.GravityGenerator;

		public override void Dispose()
		{
		}

		public override void Execute()
		{
			foreach (var entity in _query)
			{
				ref Force force = ref entity.GetForce();
				Mass mass = entity.GetMass();
				Gravity gravity = entity.GetGravity();

				if (mass.InverseMass == 0) continue;
				force.Value += gravity.Value * mass.MassValue;
			}
		}

		public override void Setup()
		{
			_query.Setup(this);
		}
	}
}
