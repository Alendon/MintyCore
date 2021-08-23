using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

using MintyCore.Components.Common;
using MintyCore.Components.Common.Physic.Dynamics;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.SystemGroups;
using MintyCore.Utils;

namespace MintyCore.Systems.Common.Physics.Dynamics
{
	[ExecuteInSystemGroup(typeof(PhysicSystemGroup))]
	[ExecuteBefore(typeof(CalculateAngularVelocitySystem))]
	public partial class CalculateAngularAcclerationSystem : ASystem
	{
		[ComponentQuery]
		TorqueInertiaAcclerationQuery<(AngularAccleration, Torque), ( Inertia, Rotation)> _query = new();

		public override Identification Identification => SystemIDs.CalculateAngularAccleration;

		public override void Dispose()
		{
		}

		public override void Execute()
		{
			foreach (var entity in _query)
			{
				ref AngularAccleration accleration = ref entity.GetAngularAccleration();
				ref Torque torque = ref entity.GetTorque();
				Inertia inertia= entity.GetInertia();
				Rotation currentRotation = entity.GetRotation();

				//This should convert the inertia from local to global space/rotation
				Matrix4x4 worldSpaceInerseInertia = Matrix4x4.Multiply(inertia.InverseInertiaTensor, Matrix4x4.CreateFromQuaternion(currentRotation.Value));
				accleration.Value = Vector3.Transform(torque.Value, worldSpaceInerseInertia);
				torque.Value = Vector3.Zero;
			}
		}

		public override void Setup()
		{
			_query.Setup(this);
		}
	}
}
