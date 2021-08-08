using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MintyCore.Utils.Maths
{
	public static partial class NumericsExtensions
	{
		public static void RotateByVector(this Quaternion quaternion, Vector3 vector)
		{
			Quaternion copy = quaternion;
			copy *= new Quaternion(0, vector.X, vector.Y, vector.Z);
			quaternion.X = copy.X;
			quaternion.Y = copy.Y;
			quaternion.Z = copy.Z;
			quaternion.W = copy.W;
		}

		public static void RotateByScaledVector(this Quaternion quaternion, Vector3 vector, float scale)
		{
			Quaternion q = new(0, vector.X * scale, vector.Y * scale, vector.Z * scale);
			q *= quaternion;
			quaternion.X += q.X * 0.5f;
			quaternion.Y += q.Y * 0.5f;
			quaternion.Z += q.Z * 0.5f;
			quaternion.W += q.W * 0.5f;
		}
	}
}
