using MintyCore.Utils;

namespace MintyCore.Identifications;

/// <summary>
/// Class containing all <see cref="SixLabors.ImageSharp.Image"/> <see cref="Identification"/>
/// </summary>
public static class ImageIDs
{
    /// <summary>
    /// <see cref="Identification"/> for the left border image. Used for dynamic ui rectangle creation
    /// </summary>
    public static Identification UiBorderLeft { get; set; }

    /// <summary>
    /// <see cref="Identification"/> for the right border image. Used for dynamic ui rectangle creation
    /// </summary>
    public static Identification UiBorderRight { get; set; }

    /// <summary>
    /// <see cref="Identification"/> for the top border image. Used for dynamic ui rectangle creation
    /// </summary>
    public static Identification UiBorderTop { get; set; }

    /// <summary>
    /// <see cref="Identification"/> for the bottom border image. Used for dynamic ui rectangle creation
    /// </summary>
    public static Identification UiBorderBottom { get; set; }

    /// <summary>
    /// <see cref="Identification"/> for the upper left corner image. Used for dynamic ui rectangle creation
    /// </summary>
    public static Identification UiCornerUpperLeft { get; set; }

    /// <summary>
    /// <see cref="Identification"/> for the upper right corner. Used for dynamic ui rectangle creation
    /// </summary>
    public static Identification UiCornerUpperRight { get; set; }

    /// <summary>
    /// <see cref="Identification"/> for the lower left corner. Used for dynamic ui rectangle creation
    /// </summary>
    public static Identification UiCornerLowerLeft { get; set; }

    /// <summary>
    /// <see cref="Identification"/> for the lower right corner. Used for dynamic ui rectangle creation
    /// </summary>
    public static Identification UiCornerLowerRight { get; set; }

}