using System.Collections.Generic;
using MintyCore.Utils;
using Veldrid;

namespace MintyCore.Render
{
    /// <summary>
    ///     Handler which manages all <see cref="Material" /> and MaterialCollections
    /// </summary>
    public static class MaterialHandler
    {
        private static readonly Dictionary<Identification, Material> _materials = new();
        private static readonly Dictionary<Identification, Material[]> _materialCollections = new();

        internal static void AddMaterial(Identification materialId, Pipeline pipeline,
            params (ResourceSet resourceSet, uint slot)[] resourceSets)
        {
            _materials.Add(materialId, new Material(pipeline, resourceSets));
        }

        internal static void AddMaterialCollection(Identification materialCollectionId,
            params Identification[] materials)
        {
            var materialCollection = new Material[materials.Length];
            for (var i = 0; i < materials.Length; i++) materialCollection[i] = _materials[materials[i]];
            _materialCollections.Add(materialCollectionId, materialCollection);
        }

        /// <summary>
        ///     Get a <see cref="Material" /> by the associated <see cref="Identification" />
        /// </summary>
        public static Material GetMaterial(Identification id)
        {
            return _materials[id];
        }

        /// <summary>
        ///     Get a MaterialCollection by the associated <see cref="Identification" />
        /// </summary>
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