using System.Collections.Generic;
using MintyCore.Utils;
using Veldrid;

namespace MintyCore.Render
{
    public static class ResourceSetHandler
    {
        private static Dictionary<Identification, ResourceSet> _resourceSets = new();

        internal static void AddResourceSet(Identification setId, ResourceLayout layout, params BindableResource[] bindableResources)
        {
            ResourceSetDescription setDescription = new(layout, bindableResources);
            
        }
    }
}