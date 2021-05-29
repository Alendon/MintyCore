using Ara3D;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace TechardryCoreSharp.Render
{
	public struct DefaultVertice : IVertice
	{
		public Vector3 Position;
		public Vector3 Color;
		public Vector3 Normal;
		public Vector2 UV;

		public VertexLayoutDescription GetVertexLayout()
		{
			return new VertexLayoutDescription(
				new VertexElementDescription( "Position", VertexElementFormat.Float3, VertexElementSemantic.Position ),
				new VertexElementDescription( "Color", VertexElementFormat.Float3, VertexElementSemantic.Color ),
				new VertexElementDescription( "Normal", VertexElementFormat.Float3, VertexElementSemantic.Normal ),
				new VertexElementDescription( "UV", VertexElementFormat.Float2, VertexElementSemantic.TextureCoordinate ) );
		}
	}
}
