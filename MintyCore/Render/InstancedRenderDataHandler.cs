using System.Collections.Generic;
using MintyCore.Utils;

namespace MintyCore.Render;

public static class InstancedRenderDataHandler
{
    private static Dictionary<Identification, (Mesh, Material[])> _instancedData = new();


    internal static void AddMeshMaterial(Identification id, Mesh mesh, Material[] material)
    {
        _instancedData.Add(id, (mesh, material));
    }

    public static (Mesh, Material[]) GetMeshMaterial(Identification id)
    {
        return _instancedData[id];
    }

    internal static void Clear()
    {
        _instancedData.Clear();
    }
}