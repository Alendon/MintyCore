using MintyCore.Utils;

namespace MintyCore.Identifications;

/// <summary>
///     <see langword="static" /> partial class which contains all <see cref="Silk.NET.Vulkan.Pipeline" /> ids
/// </summary>
public static class PipelineIDs
{
    /// <summary>
    /// <see cref="Identification"/> for a simple Color Pipeline
    /// </summary>
    public static Identification Color { get; set; }
        
    /// <summary>
    /// <see cref="Identification"/> for a simple Textured pipeline
    /// </summary>
    public static Identification Texture { get; set; }
    
    /// <summary>
    /// <see cref="Identification"/> for the ui overlay pipeline
    /// </summary>
    public static Identification UiOverlay { get; set; }
}