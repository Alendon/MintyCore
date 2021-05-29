using Ara3D;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechardryCoreSharp.Components;
using TechardryCoreSharp.Components.Common;
using TechardryCoreSharp.ECS;
using TechardryCoreSharp.SystemGroups;
using TechardryCoreSharp.Utils;

namespace TechardryCoreSharp.Systems.Common
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

				transform.Value = Matrix4x4.CreateTranslation( position.Value ) * Matrix4x4.CreateFromQuaternion( rotation.Value ) * Matrix4x4.CreateScale( scale.Value );
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
