using System.Collections.Generic;
using MintyCore.Identifications;
using MintyCore.Registries;
using MintyCore.Utils;
using MintyVeldrid;
using MintyVeldrid.ImageSharp;

namespace MintyCore.Render
{
	/// <summary>
	///     Class to handle <see cref="Texture" />. Including <see cref="TextureView" />, <see cref="Sampler" /> and Texture
	///     <see cref="ResourceSet" />
	/// </summary>
	public static class TextureHandler
    {
        internal static Dictionary<Identification, Texture> Textures = new();
        internal static Dictionary<Identification, TextureView> TextureViews = new();
        internal static Dictionary<Identification, Sampler> Samplers = new();
        internal static Dictionary<Identification, ResourceSet> TextureBindResourceSet = new();

        /// <summary>
        ///     Get a Texture
        /// </summary>
        /// <param name="textureId"></param>
        /// <returns></returns>
        public static Texture GetTexture(Identification textureId)
        {
            return Textures[textureId];
        }

        /// <summary>
        ///     Get a TextureView
        /// </summary>
        /// <param name="textureId"></param>
        /// <returns></returns>
        public static TextureView GetTextureView(Identification textureId)
        {
            return TextureViews[textureId];
        }

        /// <summary>
        ///     Get a Sampler
        /// </summary>
        /// <param name="textureId"></param>
        /// <returns></returns>
        public static Sampler GetSampler(Identification textureId)
        {
            return Samplers[textureId];
        }


        /// <summary>
        ///     Get TextureResourceSet
        /// </summary>
        /// <param name="texture"></param>
        /// <returns></returns>
        public static ResourceSet GetTextureBindResourceSet(Identification texture)
        {
            return TextureBindResourceSet[texture];
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

            Textures.Add(textureId, texture);
            TextureViews.Add(textureId, textureView);
            Samplers.Add(textureId, sampler);
            TextureBindResourceSet.Add(textureId, samplerSet);
        }


        internal static void Clear()
        {
            foreach (var textureView in TextureViews.Values) textureView.Dispose();

            foreach (var texture in Textures.Values) texture.Dispose();

            foreach (var sampler in Samplers.Values) sampler.Dispose();

            foreach (var resourceSet in TextureBindResourceSet.Values) resourceSet.Dispose();

            TextureViews.Clear();
            Textures.Clear();
            Samplers.Clear();
            TextureBindResourceSet.Clear();
        }
    }
}