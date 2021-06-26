using Ara3D;
using MintyCore.Components.Common;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MintyCore.Systems.Common
{
	class RotatorTestSystem : ASystem
	{
		public override Identification Identification => SystemIDs.Rotator;

		ComponentQuery rotatorQuery = new();

		public override void Dispose()
		{
			
		}

		public override void Execute()
		{
			foreach (var item in rotatorQuery)
			{
				ref var rotationComp = ref item.GetComponent<Rotation>();
				var rotatorComp = item.GetReadOnlyComponent<Rotator>();

				rotationComp.Value = new Vector3(
					rotationComp.Value.X + (rotatorComp.xSpeed * (float)MintyCore.DeltaTime),
					rotationComp.Value.Y + (rotatorComp.ySpeed * (float)MintyCore.DeltaTime),
					rotationComp.Value.Z + (rotatorComp.zSpeed * (float)MintyCore.DeltaTime));


			}
		}

		public override void Setup()
		{
			rotatorQuery.WithReadOnlyComponents(ComponentIDs.Rotator);
			rotatorQuery.WithComponents(ComponentIDs.Rotation);
			rotatorQuery.Setup(this);
		}
	}
}
