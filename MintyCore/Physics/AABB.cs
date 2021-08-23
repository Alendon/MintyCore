using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MintyCore.Physics
{
	public struct AABB
	{
		public Vector3 Min;
		public Vector3 Max;

		public AABB(Vector3 min, Vector3 max)
		{
			Min = min;
			Max = max;
		}
	}
}
