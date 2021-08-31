using MintyCore.Utils;

namespace MintyCore.Identifications
{
	/// <summary>
	///     <see langword="static" /> partial class which contains all <see cref="Veldrid.ResourceLayout" /> ids
	/// </summary>
	public static class ResourceLayoutIDs
    {
	    /// <summary>
	    ///     <see cref="Identification" /> of the Camera <see cref="Veldrid.ResourceLayout" />
	    /// </summary>
	    public static Identification Camera { get; internal set; }

	    /// <summary>
	    ///     <see cref="Identification" /> of the Transform <see cref="Veldrid.ResourceLayout" />
	    /// </summary>
	    public static Identification Transform { get; internal set; }

	    /// <summary>
	    ///     <see cref="Identification" /> of the Sampler <see cref="Veldrid.ResourceLayout" />
	    /// </summary>
	    public static Identification Sampler { get; internal set; }
    }
}