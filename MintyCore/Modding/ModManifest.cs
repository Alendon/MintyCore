using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;

namespace MintyCore.Modding;

/// <summary>
/// Mod manifest
/// </summary>
[PublicAPI]
public class ModManifest
{
    /// <summary>
    /// Name of the mod
    /// </summary>
    public string Name { get; internal set; } = string.Empty;

    /// <summary>
    /// Version of the mod
    /// </summary>
    public Version Version { get; internal set; } = new();

    /// <summary>
    /// String identifier of the mod
    /// </summary>
    public string Identifier { get; internal set; } = string.Empty;

    /// <summary>
    /// Description of the mod
    /// </summary>
    public string Description { get; internal set; } = string.Empty;

    /// <summary>
    /// Authors of the mod
    /// </summary>
    public string[] Authors { get; internal set; } = Array.Empty<string>();

    /// <summary>
    /// Dependencies of the mod
    /// </summary>
    public List<string> ModDependencies { get; internal set; } = new();

    internal List<ExternalDependency> ExternalDependencies { get; set; } = new();

    /// <summary>
    /// Is this mod the root mod?
    /// </summary>
    public bool IsRootMod { get; internal set; }

    internal FileInfo? ModFile { get; set; }

    internal ModManifest()
    {
    }
}