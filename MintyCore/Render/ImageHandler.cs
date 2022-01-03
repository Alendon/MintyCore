using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MintyCore.Identifications;
using MintyCore.Registries;
using MintyCore.Utils;
using Silk.NET.Vulkan;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using Image = SixLabors.ImageSharp.Image;

namespace MintyCore.Render
{
    /// <summary>
    ///     Class to handle <see cref="Texture" />. Including <see cref="TextureView" />, <see cref="Sampler" /> and Texture
    ///     <see cref="ResourceSet" />
    /// </summary>
    public static class ImageHandler
    {
        private static readonly Dictionary<Identification, Texture> _textures = new();
        private static readonly Dictionary<Identification, ImageView> _textureViews = new();
        private static readonly Dictionary<Identification, Sampler> _samplers = new();
        private static readonly Dictionary<Identification, DescriptorSet> _textureBindDescriptorSets = new();

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
        public static ImageView GetTextureView(Identification textureId)
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
        public static DescriptorSet GetTextureBindResourceSet(Identification texture)
        {
            return _textureBindDescriptorSets[texture];
        }


        internal static unsafe void AddTexture(Identification textureId, bool mipMapping, IResampler resampler)
        {
            var image = Image.Load<Rgba32>(RegistryManager.GetResourceFileName(textureId));

            Image<Rgba32>[] images = mipMapping ? MipmapHelper.GenerateMipmaps(image, resampler) : new[] { image };

            TextureDescription description = TextureDescription.Texture2D((uint)image.Width, (uint)image.Height,
                (uint)images.Length, 1, Format.R8G8B8A8Unorm, TextureUsage.STAGING);

            Texture stagingTexture = new Texture(ref description);
            description.Usage = TextureUsage.SAMPLED;
            Texture actualTexture = new Texture(ref description);


            var mapped = MemoryManager.Map(stagingTexture.MemoryBlock);
            for (int i = 0; i < images.Length; i++)
            {
                var currentImage = images[i];
                if (!currentImage.TryGetSinglePixelSpan(out var pixelSpan))
                {
                    Logger.WriteLog("Unable to get image pixelspan", LogImportance.EXCEPTION, "Render");
                }

                var layout = stagingTexture.GetSubresourceLayout((uint)i);
                var rowWidth = (uint)(currentImage.Width * 4);
                if (rowWidth == layout.RowPitch)
                {
                    Unsafe.CopyBlock(ref Unsafe.AsRef<byte>(mapped.ToPointer()),
                        ref Unsafe.As<Rgba32, byte>(ref pixelSpan.GetPinnableReference()),
                        (uint)(currentImage.Width * currentImage.Height * 4));
                }
                else
                {
                    for (uint y = 0; y < currentImage.Height; y++)
                    {
                        ref byte dstStart = ref Unsafe.Add(ref Unsafe.AsRef<byte>(mapped.ToPointer()),
                            new IntPtr((long)(y * layout.RowPitch)));
                        ref byte srcStart = ref Unsafe.Add(ref Unsafe.AsRef<byte>(mapped.ToPointer()),
                            new IntPtr(y * rowWidth));
                        Unsafe.CopyBlock(ref dstStart, ref srcStart, rowWidth);
                    }
                }
            }

            MemoryManager.UnMap(stagingTexture.MemoryBlock);

            CommandBuffer buffer = VulkanEngine.GetSingleTimeCommandBuffer();
            for (uint i = 0; i < images.Length; i++)
            {
                Texture.CopyTo(buffer, (stagingTexture, 0, 0, 0, i, 0), (actualTexture, 0, 0, 0, i, 0),
                    (uint)images[i].Width, (uint)images[i].Height, 1, 1);
            }

            VulkanEngine.ExecuteSingleTimeCommandBuffer(buffer);

            stagingTexture.Dispose();

            ImageViewCreateInfo imageViewCreateInfo = new()
            {
                SType = StructureType.ImageViewCreateInfo,
                Format = actualTexture.Format,
                Image = actualTexture.Image,
                SubresourceRange = new()
                {
                    AspectMask = ImageAspectFlags.ImageAspectColorBit,
                    LayerCount = 1,
                    BaseArrayLayer = 0,
                    LevelCount = actualTexture.MipLevels,
                    BaseMipLevel = 0
                },
                ViewType = ImageViewType.ImageViewType2D
            };

            VulkanUtils.Assert(VulkanEngine.Vk.CreateImageView(VulkanEngine.Device, in imageViewCreateInfo,
                VulkanEngine.AllocationCallback, out var imageView));

            SamplerCreateInfo samplerCreateInfo = new()
            {
                AnisotropyEnable = Vk.True,
                MaxAnisotropy = 4,
                AddressModeU = SamplerAddressMode.Repeat,
                AddressModeV = SamplerAddressMode.Repeat,
                AddressModeW = SamplerAddressMode.Repeat,
                BorderColor = BorderColor.FloatOpaqueBlack,
                SType = StructureType.SamplerCreateInfo,
                MinFilter = Filter.Linear,
                MagFilter = Filter.Linear,
                MipmapMode = SamplerMipmapMode.Linear,
                CompareOp = CompareOp.Never,
                CompareEnable = Vk.True,
                MinLod = 0,
                MaxLod = actualTexture.MipLevels
            };
            VulkanUtils.Assert(VulkanEngine.Vk.CreateSampler(VulkanEngine.Device, in samplerCreateInfo,
                VulkanEngine.AllocationCallback, out var sampler));

            var descriptorSet = DescriptorSetHandler.AllocateDescriptorSet(DescriptorSetIDs.Texture);

            DescriptorImageInfo descriptorImageInfo = new()
            {
                Sampler = sampler,
                ImageLayout = ImageLayout.ShaderReadOnlyOptimal,
                ImageView = imageView
            };

            WriteDescriptorSet writeDescriptorSet = new()
            {
                SType = StructureType.WriteDescriptorSet,
                DescriptorCount = 1,
                DescriptorType = DescriptorType.CombinedImageSampler,
                DstBinding = 0,
                DstSet = descriptorSet,
                PImageInfo = &descriptorImageInfo,
            };

            VulkanEngine.Vk.UpdateDescriptorSets(VulkanEngine.Device, 1, in writeDescriptorSet, 0, null);

            _textures.Add(textureId, actualTexture);
            _textureViews.Add(textureId, imageView);
            _samplers.Add(textureId, sampler);
            _textureBindDescriptorSets.Add(textureId, descriptorSet);
        }


        internal static unsafe void Clear()
        {
            foreach (var textureView in _textureViews.Values)
            {
                VulkanEngine.Vk.DestroyImageView(VulkanEngine.Device, textureView, VulkanEngine.AllocationCallback);
            }

            foreach (var texture in _textures.Values) texture.Dispose();

            foreach (var sampler in _samplers.Values)
            {
                VulkanEngine.Vk.DestroySampler(VulkanEngine.Device, sampler, VulkanEngine.AllocationCallback);
            }

            foreach (var descriptorSet in _textureBindDescriptorSets.Values)
            {
                DescriptorSetHandler.FreeDescriptorSet(descriptorSet);
            }

            _textureViews.Clear();
            _textures.Clear();
            _samplers.Clear();
            _textureBindDescriptorSets.Clear();
        }
    }
}