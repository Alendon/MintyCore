using System.Collections.Generic;
using MintyCore.Identifications;
using MintyCore.Registries;
using MintyCore.Utils;

namespace MintyCore.Render
{
	/// <summary>
	///     Class to handle <see cref="Texture" />. Including <see cref="TextureView" />, <see cref="Sampler" /> and Texture
	///     <see cref="ResourceSet" />
	/// </summary>
	public static class TextureHandler
    {
        /*private static readonly Dictionary<Identification, Texture> _textures = new();
        private static readonly Dictionary<Identification, TextureView> _textureViews = new();
        private static readonly Dictionary<Identification, Sampler> _samplers = new();
        private static readonly Dictionary<Identification, ResourceSet> _textureBindResourceSet = new();

        /// <summary>
        ///     Get a Texture
        /// </summary>
        /// <param name="textureId"></param>
        /// <returns></returns>
        public static Texture GetTexture(Identification textureId)
        {
            return _textures[textureId];
        }

        /// <summary>
        ///     Get a TextureView
        /// </summary>
        /// <param name="textureId"></param>
        /// <returns></returns>
        public static TextureView GetTextureView(Identification textureId)
        {
            return _textureViews[textureId];
        }

        /// <summary>
        ///     Get a Sampler
        /// </summary>
        /// <param name="textureId"></param>
        /// <returns></returns>
        public static Sampler GetSampler(Identification textureId)
        {
            return _samplers[textureId];
        }


        /// <summary>
        ///     Get TextureResourceSet
        /// </summary>
        /// <param name="texture"></param>
        /// <returns></returns>
        public static ResourceSet GetTextureBindResourceSet(Identification texture)
        {
            return _textureBindResourceSet[texture];
        }

        internal static void AddTexture(Identification textureId)
        {
            var imageLocation = RegistryManager.GetResourceFileName(textureId);
            var image = new ImageSharpTexture(imageLocation);

            var texture = image.CreateDeviceTexture(VulkanEngine.GraphicsDevice, VulkanEngine.ResourceFactory);

            TextureViewDescription viewDescription = new()
            {
                ArrayLayers = texture.ArrayLayers,
                BaseArrayLayer = 0,
                BaseMipLevel = 0,
                Format = texture.Format,
                MipLevels = texture.MipLevels,
                Target = texture
            };

            var textureView = VulkanEngine.ResourceFactory.CreateTextureView(ref viewDescription);

            SamplerDescription samplerDescription = new()
            {
                AddressModeU = SamplerAddressMode.Wrap,
                AddressModeV = SamplerAddressMode.Wrap,
                AddressModeW = SamplerAddressMode.Wrap,
                ComparisonKind = ComparisonKind.Never,
                Filter = SamplerFilter.Anisotropic,
                MaximumAnisotropy = 4,
                MaximumLod = texture.MipLevels,
                MinimumLod = 0
            };
            var sampler = VulkanEngine.ResourceFactory.CreateSampler(ref samplerDescription);


            ResourceSetDescription samplerSetDescription = new()
            {
                BoundResources = new BindableResource[] { sampler, textureView },
                Layout = ResourceLayoutHandler.GetResourceLayout(ResourceLayoutIDs.Sampler)
            };

            var samplerSet = VulkanEngine.ResourceFactory.CreateResourceSet(ref samplerSetDescription);

            _textures.Add(textureId, texture);
            _textureViews.Add(textureId, textureView);
            _samplers.Add(textureId, sampler);
            _textureBindResourceSet.Add(textureId, samplerSet);
        }


        internal static void Clear()
        {
            foreach (var textureView in _textureViews.Values) textureView.Dispose();

            foreach (var texture in _textures.Values) texture.Dispose();

            foreach (var sampler in _samplers.Values) sampler.Dispose();

            foreach (var resourceSet in _textureBindResourceSet.Values) resourceSet.Dispose();

            _textureViews.Clear();
            _textures.Clear();
            _samplers.Clear();
            _textureBindResourceSet.Clear();
        }*/
    }
}