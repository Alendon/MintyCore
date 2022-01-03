using System.Collections.Generic;
using System.Runtime.InteropServices;
using MintyCore.Utils;
using MintyCore.Utils.UnmanagedContainers;
using Silk.NET.Vulkan;

namespace MintyCore.Render
{
    /// <summary>
    ///     Handler which manages all <see cref="Material" /> and MaterialCollections
    /// </summary>
    public static class MaterialHandler
    {
        private static readonly Dictionary<Identification, Material> _materials = new();

        internal static void AddMaterial(Identification materialId, Identification pipeline,
            params (DescriptorSet, uint)[] descriptorSetArr)
        {
            UnmanagedArray<(DescriptorSet,uint)> descriptorSets = new(descriptorSetArr.Length);
            for (int i = 0; i < descriptorSets.Length; i++)
            {
                descriptorSets[i] = descriptorSetArr[i];
            }

            var material = new Material(materialId, PipelineHandler.GetPipeline(pipeline),
                PipelineHandler.GetPipelineLayout(pipeline), descriptorSets);
            _materials.Add(materialId, material);
        }


        /// <summary>
        ///     Get a <see cref="Material" /> by the associated <see cref="Identification" />
        /// </summary>
        public static Material GetMaterial(Identification id)
        {
            return _materials[id];
        }

        internal static void Clear()
        {
            foreach (var materialHandles in _materials.Values)
            {
                materialHandles.DescriptorSets.DecreaseRefCount();
            }

            _materials.Clear();
        }
    }
}