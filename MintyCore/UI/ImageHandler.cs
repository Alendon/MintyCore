using System.Collections.Generic;
using MintyCore.Modding;
using MintyCore.Utils;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace MintyCore.UI;

/// <summary>
///     Class to handle images
/// </summary>
public static class ImageHandler
{
    private static readonly Dictionary<Identification, Image<Rgba32>> _images = new();

    internal static void AddImage(Identification imageId)
    {
        var image = Image.Load<Rgba32>(RegistryManager.GetResourceFileName(imageId));
        _images.Add(imageId, image);
    }

    internal static void Clear()
    {
        _images.Clear();
    }

    /// <summary>
    ///     Get a image
    /// </summary>
    /// <param name="imageId"><see cref="Identification" /> of the image</param>
    /// <returns></returns>
    public static Image<Rgba32> GetImage(Identification imageId)
    {
        return _images[imageId];
    }

    internal static void RemoveImage(Identification objectId)
    {
        if (_images.Remove(objectId, out var image)) image.Dispose();
    }
}