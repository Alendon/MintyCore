using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using BulletSharp;
using BVector3 = BulletSharp.Math.Vector3;
using BMatrix = BulletSharp.Math.Matrix;

using MintyCore.Components.Common;
using MintyCore.Components.Common.Physic.Collisions;
using MintyCore.Components.Common.Physic.Dynamics;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Physics.Native;
using MintyCore.SystemGroups;
using MintyCore.Utils;


namespace MintyCore.Systems.Common.Physics
{
	[ExecuteInSystemGroup(typeof(PhysicSystemGroup))]
	public partial class CollisionSystem : ASystem
	{
		public override Identification Identification => SystemIDs.Collision;

		[ComponentQuery]
		private CollisionQuery<Collider, (Transform, Mass)> _query = new();

		[ComponentQuery] private CollisionApplyQuery<(Position, Rotation), Collider> _squery = new();

		private Stopwatch physic = new();
		
		public override void Setup()
		{
			_query.Setup(this);
			_squery.Setup(this);
			
			physic.Start();
		}
		
		public override void Dispose()
		{

		}

		public override unsafe void Execute()
		{
			foreach (var entity in _query)
			{
				ref Collider collider = ref entity.GetCollider();
				if (collider.nativeBody == IntPtr.Zero)
				{
					var transform = entity.GetTransform();
					var massComponent = entity.GetMass();
					

					Matrix4x4.Decompose(transform.Value, out Vector3 scale, out var rot, out var pos);
					Matrix4x4 matrix =
						Matrix4x4.CreateTranslation( pos) *
						Matrix4x4.CreateFromQuaternion(rot);
					
					var halfScale = scale / 2f;
					var shape = new BoxShape(*(BVector3*)&halfScale);
					var mass = massComponent.InverseMass == 0 ? 0 : 1 / massComponent.InverseMass;
					var motionState = new DefaultMotionState(*(BMatrix*)&matrix);
					var constructionInfo = new RigidBodyConstructionInfo(mass,
						motionState, shape, shape.CalculateLocalInertia(mass));
					var body = new RigidBody(constructionInfo);
					World.PhysicsWorld._world.AddRigidBody(body);
					constructionInfo.Dispose();
					collider.nativeBody = body.Native;
					collider.nativeMotionState = motionState.Native;

					//TODO Uncomment
					//collider.CollisionObject = new(body.Native);
					//collider.CollisionShape = new(shape.Native);
					//collider.MotionState = new(motionState.Native);
				}
			}
			
			physic.Stop();
			World.PhysicsWorld._world.StepSimulation((float)physic.ElapsedTicks / Stopwatch.Frequency);
			physic.Restart();
			
			foreach (var entity in _squery)
			{
				Collider collider = entity.GetCollider();
				
				UnsafeNativeMethods.btMotionState_getWorldTransform(collider.nativeMotionState, out BMatrix worldTransform);
				Matrix4x4.Decompose(*(Matrix4x4*)&worldTransform,out var _, out var rotation, out var translation);
				ref Rotation rot = ref entity.GetRotation();
				ref Position pos = ref entity.GetPosition();
				rot.Value = rotation;
				pos.Value = translation;
			}
			
		}

	}
}
