using System.Collections.Generic;
using MintyCore.Registries;
using MintyCore.Utils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MintyCore.UI;

public static class ImageHandler
{
    private static Dictionary<Identification, Image<Rgba32>> _images = new();

    internal static void AddImage(Identification imageId)
    {
        var image = Image.Load<Rgba32>(RegistryManager.GetResourceFileName(imageId));
        _images.Add(imageId, image);
    }

    public static void Clear()
    {
        _images.Clear();
    }

    public static Image<Rgba32> GetImage(Identification imageId)
    {
        return _images[imageId];
    }
}