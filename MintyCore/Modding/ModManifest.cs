using System;
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
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Version of the mod
    /// </summary>
    public Version Version { get; set; } = new();

    /// <summary>
    /// String identifier of the mod
    /// </summary>
    public string Identifier { get; set; } = string.Empty;

    /// <summary>
    /// Description of the mod
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Authors of the mod
    /// </summary>
    public string[] Authors { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Dependencies of the mod
    /// </summary>
    public string[] ModDependencies { get; set; } = Array.Empty<string>();

    /// <summary>
    ///  External dependencies of the mod
    /// </summary>
    public ExternalDependency[] ExternalDependencies { get; set; } = Array.Empty<ExternalDependency>();

    /// <summary>
    /// Is this mod the root mod?
    /// </summary>
    public bool IsRootMod { get; set; }

    /// <summary>
    /// The mod file
    /// </summary>
    public FileInfo? ModFile { get; set; }
}