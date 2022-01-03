using MintyCore.Utils;

namespace MintyCore.Identifications
{
	/// <summary>
	///     <see langword="static" /> partial class which contains all <see cref="Veldrid.Shader" /> ids
	/// </summary>
	public static class ShaderIDs
    {
	    /// <summary>
	    ///     <see cref="Identification" /> of the ColorFrag <see cref="Veldrid.Shader" />
	    /// </summary>
	    public static Identification ColorFrag { get; internal set; }

	    /// <summary>
	    ///     <see cref="Identification" /> of the CommonVert <see cref="Veldrid.Shader" />
	    /// </summary>
	    public static Identification CommonVert { get; internal set; }

	    /// <summary>
	    ///     <see cref="Identification" /> of the WireframeFrag <see cref="Veldrid.Shader" />
	    /// </summary>
	    public static Identification WireframeFrag { get; internal set; }

	    /// <summary>
	    ///     <see cref="Identification" /> of the Texture <see cref="Veldrid.Shader" />
	    /// </summary>
	    public static Identification Texture { get; internal set; }

	    public static Identification TriangleVert { get; set; }
    }
}