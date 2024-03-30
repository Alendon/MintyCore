using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Avalonia.Platform;
using JetBrains.Annotations;
using MintyCore.Modding.Attributes;

namespace MintyCore.AvaloniaIntegration;

public class PreviewAssetLoader : IAssetLoader
{
    private readonly IAssetLoader _internalLoader;
    private readonly DirectoryInfo _modProjectPath;
    private readonly Dictionary<string, DirectoryInfo> _categoryDirectories = new();
    private readonly Dictionary<string, Dictionary<string, string>> _categoryEntries = new();

    public PreviewAssetLoader(IAssetLoader? originalLoader, String modProjectPath)
    {
        _internalLoader = originalLoader ?? new StandardAssetLoader();
        _modProjectPath = new DirectoryInfo(modProjectPath);

        FindFileRegistryEntries();
    }

    private void FindFileRegistryEntries()
    {
        var registryFiles = _modProjectPath.EnumerateFileSystemInfos("*.registry.json", SearchOption.TopDirectoryOnly);

        Dictionary<string, string> registerMethodInfoMapping = new();

        foreach (var registryFile in registryFiles)
        {
            var registries = JsonSerializer.Deserialize<Registry[]>(File.ReadAllText(registryFile.FullName));
            if (registries is null) continue;

            foreach (var registry in registries)
            {
                var registerMethodInfo = registry.RegisterMethodInfo;
                if (string.IsNullOrEmpty(registerMethodInfo)) continue;
                
                if (!registerMethodInfoMapping.ContainsKey(registerMethodInfo))
                {
                    var infoType = Type.GetType(registerMethodInfo);
                    if (infoType is null || !infoType.IsAssignableTo(typeof(RegisterMethodInfo))) continue;

                    var resourceFolderField = infoType.GetField("ResourceSubFolder");
                    if (resourceFolderField is null) continue;

                    var resourceFolder = resourceFolderField.GetRawConstantValue();
                    if (resourceFolder is not string folder || string.IsNullOrEmpty(folder)) continue;

                    var categoryIdField = infoType.GetField("CategoryId");
                    if (categoryIdField is null) continue;

                    var categoryIdObject = categoryIdField.GetRawConstantValue();
                    if (categoryIdObject is not string categoryId || string.IsNullOrEmpty(categoryId)) continue;

                    var categoryDirectory =
                        new DirectoryInfo(Path.Combine(_modProjectPath.FullName, "Resources", folder));
                    if (!categoryDirectory.Exists) continue;

                    _categoryDirectories[categoryId] = categoryDirectory;
                    registerMethodInfoMapping[registerMethodInfo] = categoryId;
                }

                var category = registerMethodInfoMapping[registerMethodInfo];
                foreach (var entry in registry.Entries)
                {
                    if (!_categoryEntries.ContainsKey(category))
                    {
                        _categoryEntries[category] = new Dictionary<string, string>();
                    }

                    _categoryEntries[category].Add(entry.Id, entry.File);
                }
            }
        }
    }

    [UsedImplicitly]
    private record Registry(string RegisterMethodInfo, FileRegistryEntry[] Entries);

    [UsedImplicitly]
    private record FileRegistryEntry(string Id, string File);

    public void SetDefaultAssembly(Assembly assembly)
    {
        _internalLoader.SetDefaultAssembly(assembly);

        File.AppendAllText(@"D:\previewAssetLoaderDebug.txt", $"SetDefaultAssembly: {assembly.FullName}\n");
    }

    public bool Exists(Uri uri, Uri? baseUri = null)
    {
        return _internalLoader.Exists(uri, baseUri);
    }

    public Stream Open(Uri uri, Uri? baseUri = null)
    {
        if (ModAssetLoader.ParseCustomUri(uri.OriginalString, out _, out var categoryId, out var objectId) &&
            _categoryDirectories.TryGetValue(categoryId, out var categoryDirectory) &&
            _categoryEntries.TryGetValue(categoryId, out var entries))
        {
            if (entries.TryGetValue(objectId, out var file))
            {
                var filePath = Path.Combine(categoryDirectory.FullName, file);
                if (File.Exists(filePath))
                {
                    return File.OpenRead(filePath);
                }
            }
        }

        return _internalLoader.Open(uri, baseUri);
    }

    public (Stream stream, Assembly assembly) OpenAndGetAssembly(Uri uri, Uri? baseUri = null)
    {
        return _internalLoader.OpenAndGetAssembly(uri, baseUri);
    }

    public Assembly? GetAssembly(Uri uri, Uri? baseUri = null)
    {
        return _internalLoader.GetAssembly(uri, baseUri);
    }

    public IEnumerable<Uri> GetAssets(Uri uri, Uri? baseUri)
    {
        return _internalLoader.GetAssets(uri, baseUri);
    }
}