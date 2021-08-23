using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

using MintyCore.Utils;
using MintyCore.Utils.UnmanagedContainers;

namespace MintyCore.Physics
{
	public struct ColliderContainer
	{
		public UnmanagedArray<Vector3> Vertices;

		public UnmanagedArray<Vector3> Faces;

		/// <summary>
		/// How the Collider is transformed
		/// </summary>
		public Matrix4x4 Transform;

		public AABB CalculateAABB(Matrix4x4 entityTransform)
		{
			Vector3 Min = new(float.MaxValue);
			Vector3 Max = new(float.MinValue);
			foreach (var vertex in Vertices)
			{
				var transformedVertex = Vector3.Transform(Vector3.Transform(vertex, Transform), entityTransform);
				if(transformedVertex.X < Min.X) Min.X = transformedVertex.X;
				if(transformedVertex.Y < Min.Y) Min.Y = transformedVertex.Y;
				if(transformedVertex.Z < Min.Z)	Min.Z = transformedVertex.Z;

				if(transformedVertex.X > Max.X) Max.X = transformedVertex.X;
				if(transformedVertex.Y > Max.Y) Max.Y = transformedVertex.Y;
				if(transformedVertex.Z > Max.Z) Max.Z = transformedVertex.Z;
			}

			return new(Min, Max);
		}
	}
}
