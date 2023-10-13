using System;
using JetBrains.Annotations;
using MintyCore.Utils;

namespace MintyCore.Modding.Attributes;

/// <summary>
/// Attribute to mark a registry class for the source generator
/// </summary>
public class RegistryAttribute : Attribute
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="id">Id of the registry/category</param>
    /// <param name="resourceFolder">Optional additional folder where resource files are stored</param>
    public RegistryAttribute([UsedImplicitly] string id, [UsedImplicitly] string? resourceFolder = null, [UsedImplicitly] GameType applicableGameType = GameType.Local)
    {
    }
}