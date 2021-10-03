using System;
using static MintyBulletSharp.UnsafeNativeMethods;

namespace MintyBulletSharp
{
	[Flags]
	public enum CpuFeatures
	{
		None = 0,
		Fma3 = 1,
		Sse41 = 2,
		NeonHpfp = 4
	}

	public static class CpuFeatureUtility
	{
		public static CpuFeatures CpuFeatures => btCpuFeatureUtility_getCpuFeatures();
	}
}
