using MintyCore.Utils;

namespace MintyCore.Identifications
{
	/// <summary>
	/// <see langword="static"/> partial class which contains all <see cref="Veldrid.Pipeline"/> ids
	/// </summary>
	public static partial class PipelineIDs
    {
		/// <summary>
		/// <see cref="Identification"/> of the Color pipeline
		/// </summary>
		public static Identification Color { get; internal set; }

		/// <summary>
		/// <see cref="Identification"/> of the WireFrame pipeline
		/// </summary>
		public static Identification WireFrame { get; internal set; }

		/// <summary>
		/// <see cref="Identification"/> of the TexturePipeline
		/// </summary>
		public static Identification Texture { get; internal set; }

    }
}