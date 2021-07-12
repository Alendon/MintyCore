using MintyCore.Identifications;
using MintyCore.Registries;
using MintyCore.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace MintyCore.Render
{
	public static class TextureHandler
	{
		internal static Dictionary<Identification, Texture> _textures = new();
		internal static Dictionary<Identification, TextureView> _textureViews = new();
		internal static Dictionary<Identification, Sampler> _samplers = new();
		internal static Dictionary<Identification, ResourceSet> _samplerResourceSet = new();

		public static Texture GetTexture(Identification textureID)
		{
			return _textures[textureID];
		}

		public static TextureView GetTextureView(Identification textureID)
		{
			return _textureViews[textureID];
		}

		public static Sampler GetSampler(Identification textureID)
		{
			return _samplers[textureID];
		}

		public static ResourceSet GetSamplerResourceSet(Identification texture)
		{
			return _samplerResourceSet[texture];
		}

		internal static void AddTexture(Identification textureID)
		{
			var imageLocation = RegistryManager.GetResourceFileName(textureID);
			var image = new Veldrid.ImageSharp.ImageSharpTexture(imageLocation);

			Texture texture = image.CreateDeviceTexture(VulkanEngine.GraphicsDevice, VulkanEngine.ResourceFactory);

			TextureViewDescription viewDescription = new()
			{
				ArrayLayers = texture.ArrayLayers,
				BaseArrayLayer = 0,
				BaseMipLevel = 0,
				Format = texture.Format,
				MipLevels = texture.MipLevels,
				Target = texture
			};

			TextureView textureView = VulkanEngine.ResourceFactory.CreateTextureView(ref viewDescription);

			SamplerDescription samplerDescription = new()
			{
				AddressModeU = SamplerAddressMode.Wrap,
				AddressModeV = SamplerAddressMode.Wrap,
				AddressModeW = SamplerAddressMode.Wrap,
				ComparisonKind = ComparisonKind.Never,
				Filter = SamplerFilter.Anisotropic,
				MaximumAnisotropy = 16,
				MaximumLod = texture.MipLevels,
				MinimumLod = 0
			};
			Sampler sampler = VulkanEngine.ResourceFactory.CreateSampler(ref samplerDescription);
			

			ResourceSetDescription samplerSetDescription = new()
			{
				BoundResources = new BindableResource[] { sampler, textureView },
				Layout = ResourceLayoutHandler.GetResourceLayout(ResourceLayoutIDs.Sampler)
			};
			
			ResourceSet samplerSet = VulkanEngine.ResourceFactory.CreateResourceSet(ref samplerSetDescription);

			_textures.Add(textureID, texture);
			_textureViews.Add(textureID, textureView);
			_samplers.Add(textureID, sampler);
			_samplerResourceSet.Add(textureID, samplerSet);
		}


		internal static void Clear()
		{
			foreach (var textureView in _textureViews.Values)
			{
				textureView.Dispose();
			}

			foreach (var texture in _textures.Values)
			{
				texture.Dispose();
			}

			foreach (var sampler in _samplers.Values)
			{
				sampler.Dispose();
			}

			foreach (var resourceSet in _samplerResourceSet.Values)
			{
				resourceSet.Dispose();
			}

			_textureViews.Clear();
			_textures.Clear();
			_samplers.Clear();
			_samplerResourceSet.Clear();
		}


	}
}
