using System.Collections.Generic;
using System.Runtime.InteropServices;
using MintyCore.Utils;

namespace MintyCore.Render
{
    /// <summary>
    ///     Handler which manages all <see cref="Material" /> and MaterialCollections
    /// </summary>
    public static class MaterialHandler
    {
        private static readonly Dictionary<Identification, Material> _materials = new();
        private static readonly Dictionary<Identification, GCHandle> _materialHandles = new();

        internal static void AddMaterial(Identification materialId, Pipeline pipeline,
            params (ResourceSet resourceSet, uint slot)[] resourceSets)
        {
            var material = new Material(pipeline, materialId, resourceSets);
            _materials.Add(materialId, material);
            _materialHandles.Add(materialId, GCHandle.Alloc(material, GCHandleType.Normal));
        }
        

        /// <summary>
        ///     Get a <see cref="Material" /> by the associated <see cref="Identification" />
        /// </summary>
        public static Material GetMaterial(Identification id)
        {
            return _materials[id];
        }

        /// <summary>
        /// Get a <see cref="GCHandle"/> for a <see cref="Material"/> by the associated <see cref="Identification"/>
        /// </summary>
        public static GCHandle GetMaterialHandle(Identification id)
        {
            return _materialHandles[id];
        }
        
        internal static void Clear()
        {
            foreach (var materialHandles in _materialHandles.Values)
            {
                materialHandles.Free();
            }
            
            _materialHandles.Clear();
            _materials.Clear();
        }
    }
}