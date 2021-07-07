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
					Math.Clamp( rotationComp.Value.X + (rotatorComp.xSpeed * (float)MintyCore.DeltaTime), 0f, 360f),
					Math.Clamp( rotationComp.Value.Y + (rotatorComp.ySpeed * (float)MintyCore.DeltaTime), 0f, 360f),
					Math.Clamp( rotationComp.Value.Z + (rotatorComp.zSpeed * (float)MintyCore.DeltaTime), 0f, 360f));

				rotationComp.Dirty = 1;
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
