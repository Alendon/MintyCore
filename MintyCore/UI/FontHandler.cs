using System;
using System.Collections.Generic;
using MintyCore.Registries;
using MintyCore.Utils;
using SixLabors.Fonts;

namespace MintyCore.UI;

public static class FontHandler
{
    private static readonly Dictionary<Identification, FontFamily> _fontFamilies = new();
    private static FontCollection _fontCollection = new();

    internal static void LoadFont(Identification fontId)
    {
        FontFamily family = _fontCollection.Install(RegistryManager.GetResourceFileName(fontId));
        _fontFamilies.Add(fontId,family);
    }

    public static Font GetFont(Identification fontFamilyId, int fontSize, FontStyle fontStyle = FontStyle.Regular)
    {
        var family = _fontFamilies[fontFamilyId];
        return family.CreateFont(fontSize, fontStyle);
    }

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