using Ara3D;
using MintyCore.Components.Client;
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

	class MovementSystem : ASystem
	{
		public override Identification Identification => SystemIDs.Movement;

		ComponentQuery componentQuery = new();

		public override void Dispose()
		{
		}

		public override void Execute()
		{
			foreach (var item in componentQuery)
			{
				var input = item.GetReadOnlyComponent<Input>();
				ref var position = ref item.GetComponent<Position>();

				if (input.Right.LastKeyValid)
				{

				}

				float changedX = 0, changedY = 0, changedZ = 0;
				changedX += input.Right.LastKeyValid ? 1 : 0;
				changedX += input.Left.LastKeyValid ? -1 : 0;
				changedY += input.Up.LastKeyValid ? 1 : 0;
				changedY += input.Down.LastKeyValid ? -1 : 0;
				changedZ += input.Forward.LastKeyValid ? -1 : 0;
				changedZ += input.Backward.LastKeyValid ? 1 : 0;
				changedX *= (float)MintyCore.DeltaTime * 0.01f;
				changedY *= (float)MintyCore.DeltaTime * 0.01f;
				changedZ *= (float)MintyCore.DeltaTime * 0.01f;

				Vector3 change = new(changedX, changedY, changedZ);

				position.Value += change;

		
			}
		}

		public override void Setup()
		{
			componentQuery.WithComponents(ComponentIDs.Position);
			componentQuery.WithReadOnlyComponents(ComponentIDs.Input);
			componentQuery.Setup(this);
		}
	}
}
