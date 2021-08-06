using MintyCore.Components.Common;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Utils;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MintyCore.Systems.Common
{
	partial class RotatorTestSystem : ASystem
	{
		public override Identification Identification => SystemIDs.Rotator;

		[ComponentQuery]
		ComponentQuery<Rotation, Rotator> rotatorQuery = new();

		public override void Dispose()
		{

		}

		public override void Execute()
		{
			foreach (var item in rotatorQuery)
			{
				ref var rotationComp = ref item.GetRotation();
				var rotatorComp = item.GetRotator();



				rotationComp.Value = new Vector3(
					(rotationComp.Value.X + (rotatorComp.xSpeed * (float)MintyCore.DeltaTime)),
					(rotationComp.Value.Y + (rotatorComp.ySpeed * (float)MintyCore.DeltaTime)),
					(rotationComp.Value.Z + (rotatorComp.zSpeed * (float)MintyCore.DeltaTime)));

				rotationComp.Dirty = 1;
			}

		}

		public override void Setup()
		{
			rotatorQuery.Setup(this);
		}
	}
}
