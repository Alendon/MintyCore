using Ara3D;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace TechardryCoreSharp.Render
{
	public class Mesh
	{
		public bool IsStatic { get; internal set; }
		private IntPtr _vertices;
		private ulong _dataLength;
		private DeviceBuffer _vertexBuffer;

		private Func<object, bool> _isValidVertex;
		private Func<IVertice> _getDefaultVertice; 

		private Mesh() { }
		internal static Mesh CreateStaticMesh(IntPtr vertices, ulong verticeLength )
		{
			return new Mesh()
			{
				IsStatic = true,
				_dataLength = verticeLength,
				_getDefaultVertice = () => { return new DefaultVertice(); },
				_isValidVertex = ( object obj ) => { return obj is DefaultVertice; },
				_vertices = vertices
			};
		}

	}
}
