using System;
using MintyBulletSharp.Math;
using static MintyBulletSharp.UnsafeNativeMethods;

namespace MintyBulletSharp
{
	public class ConvexTriangleMeshShape : PolyhedralConvexAabbCachingShape
	{
		public ConvexTriangleMeshShape(StridingMeshInterface meshInterface, bool calcAabb = true)
		{
			IntPtr native = btConvexTriangleMeshShape_new(meshInterface.Native, calcAabb);
			InitializeCollisionShape(native);

			MeshInterface = meshInterface;
		}

		public void CalculatePrincipalAxisTransform(Matrix principal, out Vector3 inertia,
			out float volume)
		{
			btConvexTriangleMeshShape_calculatePrincipalAxisTransform(Native, ref principal,
				out inertia, out volume);
		}

		public StridingMeshInterface MeshInterface { get; }
	}
}
