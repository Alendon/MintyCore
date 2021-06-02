using System;
using Veldrid;

namespace MintyCore.Render
{
	public interface IVertex : IEquatable<IVertex>
	{
		VertexLayoutDescription GetVertexLayout();
	}
}