using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

using MintyCore.Physics;

namespace MintyCore.Utils.Maths
{
	public static partial class PhysicCalculator
	{
		/// <summary>
		/// Check if two AABBs (in world space) overlaps
		/// </summary>
		public static bool AabbOverlap(AABB firstAabb, AABB secondAabb)
		{
			var d1X = firstAabb.Min.X - secondAabb.Max.X;
			var d2X = secondAabb.Min.X - firstAabb.Max.X;
			var d1Y = firstAabb.Min.Y - secondAabb.Max.Y;
			var d2Y = secondAabb.Min.Y - firstAabb.Max.Y;
			var d1Z = firstAabb.Min.Z - secondAabb.Max.Z;
			var d2Z = secondAabb.Min.Z - firstAabb.Max.Z;

			return !(d1X > 0 || d1Y > 0 || d1Z > 0 || d2X > 0 || d2Y > 0 || d2Z > 00);
		}
	}
}
