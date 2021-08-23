using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MintyCore.Components.Common;
using MintyCore.Components.Common.Physic.Collisions;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.SystemGroups;
using MintyCore.Utils;
using MintyCore.Utils.Maths;

namespace MintyCore.Systems.Common.Physics
{
	[ExecuteInSystemGroup(typeof(PhysicSystemGroup))]
	public partial class CollisionSystem : ASystem
	{
		public override Identification Identification => SystemIDs.Collision;

		[ComponentQuery]
		private CollisionQuery<Collider, Transform> _query = new();

		public override void Dispose()
		{

		}

		public override void Execute()
		{
			//TODO optimize the comparsion of entities

			var firstEntityEnumerator = (CollisionQuery<Collider, Transform>.Enumerator)_query.GetEnumerator();

			while (firstEntityEnumerator.MoveNext())
			{
				var firstEntity = firstEntityEnumerator.Current;

				//Copy the enumerator. This works fine as the enumerator is a value type
				var secondEntityEnumerator = firstEntityEnumerator;
				while (secondEntityEnumerator.MoveNext())
				{
					var secondEntity = secondEntityEnumerator.Current;
					CheckCollision(firstEntity, secondEntity);
				}
			}
		}

		private void CheckCollision(CollisionQuery<Collider, Transform>.CurrentEntity firstEntity, CollisionQuery<Collider, Transform>.CurrentEntity secondEntity)
		{
			ref Collider firstCollider = ref firstEntity.GetCollider();
			ref Collider secondCollider = ref secondEntity.GetCollider();

			Transform firstTransform = firstEntity.GetTransform();
			Transform secondTransform = secondEntity.GetTransform();

			firstCollider.RecalculateAABB(firstTransform);
			secondCollider.RecalculateAABB(secondTransform);

			bool aabbOverlap = PhysicCalculator.AabbOverlap(firstCollider.AABB, secondCollider.AABB);
			if (aabbOverlap)
				Logger.WriteLog("AABB overlap detected", LogImportance.DEBUG, "Physic");
		}

		public override void Setup()
		{
			_query.Setup(this);
		}
	}
}
