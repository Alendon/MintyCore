using System;
using Silk.NET.Vulkan;

namespace MintyCore.Render
{
	/// <summary>
	///     Interface for all Vertex implementations
	/// </summary>
	public interface IVertex : IEquatable<IVertex>
    {
	    /// <summary>
	    ///     Get the <see cref="VertexInputBindingDescription" /> of an <see cref="IVertex" />
	    /// </summary>
	    VertexInputBindingDescription[] GetVertexBindings();

	    VertexInputAttributeDescription[] GetVertexAttributes();


    }
}