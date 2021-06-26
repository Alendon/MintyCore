using Veldrid;

namespace MintyCore.Render
{
    public class Material
    {
        private Pipeline _pipeline;
        private (ResourceSet rs, uint slot)[] _resourceSets;

        public void BindMaterial(CommandList cl)
        {
            cl.SetPipeline(_pipeline);
            foreach (var resourceSet in _resourceSets)
            {
                cl.SetGraphicsResourceSet(resourceSet.slot, resourceSet.rs);
            }
        }

        public Material(Pipeline pipeline, params (ResourceSet resourceSet, uint slot)[] resourceSets)
        {
            _pipeline = pipeline;
            _resourceSets = resourceSets;
        }
    }
}