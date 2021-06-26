using System.Collections.Generic;
using MintyCore.Registries;
using MintyCore.Utils;
using Veldrid;

namespace MintyCore.Render
{
    public static class MaterialHandler
    {
        private static Dictionary<Identification, Material> _materials = new();
        private static Dictionary<Identification, Material[]> _materialCollections = new();

        internal static void AddMaterial(Identification materialId, Pipeline pipeline,
            params (ResourceSet resourceSet, uint slot)[] resourceSets)
        {
            _materials.Add(materialId, new Material(pipeline, resourceSets));
        }

        internal static void AddMaterialCollection(Identification materialCollectionId,
            params Identification[] materials)
        {
            Material[] materialCollection = new Material[materials.Length];
            for (var i = 0; i < materials.Length; i++)
            {
                materialCollection[i] = _materials[materials[i]];
            }
            _materialCollections.Add(materialCollectionId, materialCollection);
        }

        public static Material GetMaterial(Identification id)
        {
            return _materials[id];
        }

        public static Material[] GetMaterialCollection(Identification id)
        {
            return _materialCollections[id];
        }

        internal static void ClearCollections()
        {
            _materialCollections.Clear();
        }

        internal static void Clear()
        {
            _materials.Clear();
        }
    }
}