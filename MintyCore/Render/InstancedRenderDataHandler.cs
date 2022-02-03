using System.Collections.Generic;
using MintyCore.Utils;

namespace MintyCore.Render;

/// <summary>
/// Class to manage instanced render data
/// </summary>
public static class InstancedRenderDataHandler
{
    private static readonly Dictionary<Identification, (Mesh, Material[])> _instancedData = new();


    internal static void AddMeshMaterial(Identification id, Mesh mesh, Material[] material)
    {
        _instancedData.Add(id, (mesh, material));
    }

    /// <summary>
    /// Get the instanced render data by the <see cref="Identification"/>
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public static (Mesh, Material[]) GetMeshMaterial(Identification id)
    {
        return _instancedData[id];
    }

    internal static void Clear()
    {
        _instancedData.Clear();
    }
}