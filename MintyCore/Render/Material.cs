using MintyCore.Utils;

namespace MintyCore.Render
{
    /// <summary>
    ///     The Material is a collection of different graphics resources describing how to render something
    /// </summary>
    public class Material
    {
        /*private readonly Pipeline _pipeline;
        private readonly (ResourceSet rs, uint slot)[] _resourceSets;*/
        
        public Identification MaterialId { get; private init; }


        /*internal Material(Pipeline pipeline, Identification materialId, params (ResourceSet resourceSet, uint slot)[] resourceSets)
        {
            _pipeline = pipeline;
            _resourceSets = resourceSets;
            MaterialId = materialId;
        }

        /// <summary>
        ///     Bind the Material to the <paramref name="cl" />
        /// </summary>
        public void BindMaterial(CommandList cl)
        {
            cl.SetPipeline(_pipeline);
            foreach (var (resourceSet, slot) in _resourceSets) cl.SetGraphicsResourceSet(slot, resourceSet);
        }*/
    }
}