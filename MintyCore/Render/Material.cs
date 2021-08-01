using Veldrid;

namespace MintyCore.Render
{
    /// <summary>
    /// The Material is a collection of different graphics ressources describing how to render something
    /// </summary>
    public class Material
    {
        private Pipeline _pipeline;
        private (ResourceSet rs, uint slot)[] _resourceSets;

        /// <summary>
        /// Bind the Material to the <paramref name="cl"/>
        /// </summary>
        public void BindMaterial(CommandList cl)
        {
            cl.SetPipeline(_pipeline);
            foreach (var resourceSet in _resourceSets)
            {
                cl.SetGraphicsResourceSet(resourceSet.slot, resourceSet.rs);
            }
        }


        internal Material(Pipeline pipeline, params (ResourceSet resourceSet, uint slot)[] resourceSets)
        {
            _pipeline = pipeline;
            _resourceSets = resourceSets;
        }
    }
}