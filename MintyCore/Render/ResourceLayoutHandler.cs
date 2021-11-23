using System.Collections.Generic;
using MintyCore.Utils;

namespace MintyCore.Render
{
	/// <summary>
	///     Class to handle <see cref="ResourceLayout" />
	/// </summary>
	public static class ResourceLayoutHandler
    {
        private static readonly Dictionary<Identification, ResourceLayout> _resourceLayouts = new();

        internal static void AddResourceLayout(Identification id, ref ResourceLayoutDescription description)
        {
            _resourceLayouts.Add(id, VulkanEngine.ResourceFactory.CreateResourceLayout(ref description));
        }

        /// <summary>
        ///     Get a <see cref="ResourceLayout" />
        /// </summary>
        /// <param name="resourceLayoutId"></param>
        /// <returns></returns>
        public static ResourceLayout GetResourceLayout(Identification resourceLayoutId)
        {
            return _resourceLayouts[resourceLayoutId];
        }

        internal static void Clear()
        {
            foreach (var resourceLayout in _resourceLayouts.Values) resourceLayout.Dispose();

            _resourceLayouts.Clear();
        }
    }
}