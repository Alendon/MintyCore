using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

using MintyCore.Components.Common;
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
	[ExecuteBefore(typeof(CalculateLinearAcclerationSystem), typeof(CalculateAngularAcclerationSystem))]
	public partial class SpringGeneratorSystem : ASystem
	{
		[ComponentQuery]
		SpringForceQuery<(Force,Torque), (Spring, Transform)> _query = new();

		public override Identification Identification => SystemIDs.SpringGenerator;

		public override void Dispose()
		{
		}

		public override void Execute()
		{
			foreach (var entity in _query)
			{
				ref Force force = ref entity.GetForce();
				ref Torque torque = ref entity.GetTorque();
				Spring spring = entity.GetSpring();
				Transform transform = entity.GetTransform();

				var connectionWorldPoint = Vector3.Transform(spring.LocalPoint, transform.Value);
				var difference = spring.WorldPoint - connectionWorldPoint;

				var magnitude = difference.Length();
				magnitude = MathF.Abs(magnitude - spring.SpringLength);
				magnitude *= spring.SpringConstant;

				difference = Vector3.Normalize(difference);
				difference *= magnitude;

				force.Value += difference;
				torque.Value += Vector3.Cross(spring.LocalPoint, difference);
			}
		}

		public override void Setup()
		{
			_query.Setup(this);
		}
	}
}
