using System;
using System.Collections.Generic;
using MintyCore.Registries;
using MintyCore.Utils;
using SixLabors.Fonts;

namespace MintyCore.UI;

public static class FontHandler
{
    public static IntPtr FontLibrary { get; private set; }

    private static readonly Dictionary<Identification, Font> _fonts = new();

    internal static void LoadFont(Identification fontId, uint fontSize = 6)
    {
        FontCollection collection = new FontCollection();
        FontFamily family = collection.Install(RegistryManager.GetResourceFileName(fontId));
        Font font = family.CreateFont(fontSize, FontStyle.Italic);
        _fonts.Add(fontId,font);
    }

    public static Font GetFont(Identification fontId)
    {
        return _fonts[fontId];
    }

    internal static void Clear()
    {
        _fonts.Clear();
    }
}