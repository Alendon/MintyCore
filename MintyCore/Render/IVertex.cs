using System;
using Veldrid;

namespace MintyCore.Render
{
	/// <summary>
	///     Interface for all Vertex implementations
	/// </summary>
	public interface IVertex : IEquatable<IVertex>
    {
	    /// <summary>
	    ///     Get the <see cref="VertexLayoutDescription" /> of an <see cref="IVertex" />
	    /// </summary>
	    VertexLayoutDescription GetVertexLayout();
    }
}