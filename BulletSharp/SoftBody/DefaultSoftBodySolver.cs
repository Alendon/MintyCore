using System;
using static MintyBulletSharp.UnsafeNativeMethods;

namespace MintyBulletSharp.SoftBody
{
	public class DefaultSoftBodySolver : SoftBodySolver
	{
		public DefaultSoftBodySolver()
		{
			IntPtr native = btDefaultSoftBodySolver_new();
			InitializeUserOwned(native);
		}
		/*
		public void CopySoftBodyToVertexBuffer(SoftBody softBody, VertexBufferDescriptor vertexBuffer)
		{
			btDefaultSoftBodySolver_copySoftBodyToVertexBuffer(Native, softBody._native,
				vertexBuffer._native);
		}
		*/
	}
}
