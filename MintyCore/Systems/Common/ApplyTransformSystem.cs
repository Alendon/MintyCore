using Ara3D;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MintyCore.Components;
using MintyCore.Components.Common;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.SystemGroups;
using MintyCore.Utils;

namespace MintyCore.Systems.Common
{
	[ExecuteInSystemGroup(typeof(FinalizationSystemGroup))]
	class ApplyTransformSystem : ASystem
	{
		private ComponentQuery _componentQuery = new ComponentQuery();

		public override Identification Identification => SystemIDs.ApplyTransform;

		public override void Dispose() { }
		public override void Execute()
		{
			foreach ( var entity in _componentQuery )
			{
				Position position = entity.GetReadOnlyComponent<Position>();
				Rotation rotation = entity.GetReadOnlyComponent<Rotation>();
				Scale scale = entity.GetReadOnlyComponent<Scale>();
				ref Transform transform = ref entity.GetComponent<Transform>();

				transform.Value = Matrix4x4.CreateTranslation( position.Value ) * Matrix4x4.CreateFromYawPitchRoll( rotation.Value.X, rotation.Value.Y, rotation.Value.Z) * Matrix4x4.CreateScale( scale.Value );
			}
		}

		public override void Setup()
		{
			_componentQuery.WithReadOnlyComponents( ComponentIDs.Position, ComponentIDs.Rotation, ComponentIDs.Scale );
			_componentQuery.WithComponents( ComponentIDs.Transform );
			_componentQuery.Setup( this );
		}
	}
}
