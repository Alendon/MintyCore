using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Avalonia.Platform;
using MintyCore.Modding;

namespace MintyCore.AvaloniaIntegration;

internal class ModAssetLoader : IAssetLoader
{
    private readonly IAssetLoader _originalLoader;
    private readonly IModManager _modManager;
    
    private const string CompareString = "abcdefghijklmnopqrstuvwxyz_";
    private static readonly SearchValues<char> AllowedAssetCharacters = SearchValues.Create(CompareString);

    /// <summary>
    ///  Creates a new instance of <see cref="ModAssetLoader"/>.
    /// </summary>
    public ModAssetLoader(IAssetLoader? originalLoader, IModManager modManager)
    {
        _modManager = modManager;
        _originalLoader = originalLoader ?? new StandardAssetLoader();
    }

    public void SetDefaultAssembly(Assembly assembly)
    {
        _originalLoader.SetDefaultAssembly(assembly);
    }

    public bool Exists(Uri uri, Uri? baseUri = null)
    {
        if (ParseCustomUri(uri.OriginalString, out var modName, out var categoryName, out var objectName))
        {
            return _modManager.RegistryManager.TryGetObjectId(modName, categoryName, objectName, out _);
        }

        return _originalLoader.Exists(uri, baseUri);
    }

    public Stream Open(Uri uri, Uri? baseUri = null)
    {
        
        if (ParseCustomUri(uri.OriginalString, out var modName, out var categoryName, out var objectName)
            && _modManager.RegistryManager.TryGetObjectId(modName, categoryName, objectName, out var id))
        {
            return _modManager.GetResourceFileStream(id);
        }
        
        return _originalLoader.Open(uri, baseUri);
    }

    public (Stream stream, Assembly assembly) OpenAndGetAssembly(Uri uri, Uri? baseUri = null)
    {
        if (ParseCustomUri(uri.OriginalString, out var modName, out var categoryName, out var objectName)
            && _modManager.RegistryManager.TryGetObjectId(modName, categoryName, objectName, out var id))
        {
            return (_modManager.GetResourceFileStream(id), _originalLoader.GetAssembly(uri, baseUri)
                ?? throw new InvalidOperationException("Assembly not found"));
        }

        return _originalLoader.OpenAndGetAssembly(uri, baseUri);
    }

    public Assembly? GetAssembly(Uri uri, Uri? baseUri = null)
    {
        return _originalLoader.GetAssembly(uri, baseUri);
    }

    //TODO do we need a custom implementation for this?
    public IEnumerable<Uri> GetAssets(Uri uri, Uri? baseUri)
    {
        return _originalLoader.GetAssets(uri, baseUri);
    }

    internal static bool ParseCustomUri(ReadOnlySpan<char> uri, out string modName,
        out string categoryName, out string objectName)
    {
        // parse an "uri" of the form "modName:categoryName:objectName"
        //TODO Future: Add support to use ReadOnlySpans for the results instead of strings
        
        modName = string.Empty;
        categoryName = string.Empty;
        objectName = string.Empty;

        var remaining = uri;
        

        var firstColon = remaining.IndexOf(':');
        if (firstColon == -1)
            return false;

        modName = remaining[..firstColon].ToString();
        if(modName.AsSpan().IndexOfAnyExcept(AllowedAssetCharacters) != -1)
            return false;
        
        remaining = remaining[(firstColon + 1)..];

        var secondColon = remaining.IndexOf(':');
        if (secondColon == -1)
            return false;

        categoryName = remaining[..secondColon].ToString();
        
        if(categoryName.AsSpan().IndexOfAnyExcept(AllowedAssetCharacters) != -1)
            return false;
        
        remaining = remaining[(secondColon + 1)..];

        if (remaining.IndexOf(':') != -1)
            return false;

        objectName = remaining.ToString();
        
        return objectName.AsSpan().IndexOfAnyExcept(AllowedAssetCharacters) == -1;
    }
}