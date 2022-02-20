using System.Collections.Generic;
using MintyCore.Registries;
using MintyCore.Utils;
using SixLabors.Fonts;

namespace MintyCore.UI;

/// <summary>
///     Class to handle fonts
/// </summary>
public static class FontHandler
{
    private static readonly Dictionary<Identification, FontFamily> _fontFamilies = new();
    private static FontCollection _fontCollection = new();

    internal static void LoadFont(Identification fontId)
    {
        var family = _fontCollection.Install(RegistryManager.GetResourceFileName(fontId));
        _fontFamilies.Add(fontId, family);
    }

    /// <summary>
    ///     Get a font from a font family
    /// </summary>
    /// <param name="fontFamilyId"><see cref="Identification" /> of the font family</param>
    /// <param name="fontSize">Size of the font</param>
    /// <param name="fontStyle">Style of the font</param>
    /// <returns>Created font</returns>
    public static Font GetFont(Identification fontFamilyId, int fontSize, FontStyle fontStyle = FontStyle.Regular)
    {
        var family = _fontFamilies[fontFamilyId];
        return family.CreateFont(fontSize, fontStyle);
    }

    /// <summary>
    ///     Get a font family
    /// </summary>
    /// <param name="fontFamilyId"><see cref="Identification" /> of the font family</param>
    /// <returns>The font family</returns>
    public static FontFamily GetFontFamily(Identification fontFamilyId)
    {
        return _fontFamilies[fontFamilyId];
    }

    internal static void Clear()
    {
        _fontFamilies.Clear();
        _fontCollection = new FontCollection();
    }
}