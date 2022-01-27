using System;
using System.Collections.Generic;
using FreeTypeSharp;
using FreeTypeSharp.Native;
using MintyCore.Registries;
using static FreeTypeSharp.Native.FT;
using MintyCore.Utils;

namespace MintyCore.UI;

public static class FontManager
{
    public static IntPtr FontLibrary { get; private set; }

    private static readonly Dictionary<Identification, Font> _fonts = new();

    internal static void Initialize()
    {
        var error = FT_Init_FreeType(out var library);
        if (error != FT_Error.FT_Err_Ok)
        {
            Logger.WriteLog($"Failed to initialize FreeType library: {error.ToString()}", LogImportance.EXCEPTION, "UI");
        }
    }

    internal static void LoadFont(Identification fontId, uint fontSize = 36)
    {
        var error = FT_New_Face(FontLibrary, RegistryManager.GetResourceFileName(fontId), 0, out var face);
        if (error != FT_Error.FT_Err_Ok)
        {
            Logger.WriteLog($"Failed to load font: {error.ToString()}", LogImportance.ERROR, "UI");
            return;
        }
        error = FT_Set_Pixel_Sizes(face, 0, fontSize);
        if (error != FT_Error.FT_Err_Ok)
        {
            Logger.WriteLog($"Failed to set font size: {error.ToString()}", LogImportance.ERROR, "UI");
        }
        _fonts.Add(fontId, new Font(face));
    }
}