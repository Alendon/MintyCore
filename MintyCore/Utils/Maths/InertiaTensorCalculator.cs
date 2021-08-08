using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MintyCore.Utils.Maths
{
	/// <summary>
	/// Class which contains multiple methods to calculate the InertiaMatrix
	/// </summary>
	public static partial class InertiaTensorCalculator
	{
		private const float oneTwelth = 1f / 12f;
		private const float twoFifth = 2f / 5f;
		private const float twoThird = 2f / 3f;

		/// <summary>
		/// Calculate the inertia for a cuboid with constant density
		/// </summary>
		public static Matrix4x4 Cuboid(Vector3 dimensions, float mass)
		{
			Matrix4x4 inertia = Matrix4x4.Identity;
			inertia.M11 = oneTwelth * mass * (MathF.Pow(dimensions.Y, 2) + MathF.Pow(dimensions.Z, 2));
			inertia.M22 = oneTwelth * mass * (MathF.Pow(dimensions.X, 2) + MathF.Pow(dimensions.Z, 2));
			inertia.M33 = oneTwelth * mass * (MathF.Pow(dimensions.Y, 2) + MathF.Pow(dimensions.X, 2));
			return inertia;
		}

		
		/// <summary>
		/// Calculate the inertia for a solid sphere with a constant density
		/// </summary>
		public static Matrix4x4 SolidSphere(float radius, float mass)
		{
			Matrix4x4 inertia = Matrix4x4.Identity;
			float v = twoFifth * mass * MathF.Pow(radius, 2);
			inertia.M11 = v;
			inertia.M22 = v;
			inertia.M33 = v;
			return inertia;
		}

		/// <summary>
		/// Calculate the inertia for a hollow sphere with a constant density
		/// </summary>
		public static Matrix4x4 HollowSphere(float radius, float mass)
		{
			Matrix4x4 inertia = Matrix4x4.Identity;
			float v = twoThird * mass * MathF.Pow(radius, 2);
			inertia.M11 = v;
			inertia.M22 = v;
			inertia.M33 = v;
			return inertia;
		}
	}
}
