using System;
using System.Diagnostics;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Render
{
    public static class FormatHelpers
    {
        public static uint GetSizeInBytes(Format format)
        {
            switch (format)
            {
                case Format.R8Unorm:
                case Format.R8SNorm:
                case Format.R8Uint:
                case Format.R8Sint:
                    return 1;

                case Format.R16Unorm:
                case Format.R16SNorm:
                case Format.R16Uint:
                case Format.R16Sint:
                case Format.R16Sfloat:
                case Format.R8G8Unorm:
                case Format.R8G8SNorm:
                case Format.R8G8Uint:
                case Format.R8G8Sint:
                    return 2;

                case Format.R32Uint:
                case Format.R32Sint:
                case Format.R32Sfloat:
                case Format.R16G16Unorm:
                case Format.R16G16SNorm:
                case Format.R16G16Uint:
                case Format.R16G16Sint:
                case Format.R16G16Sfloat:
                case Format.R8G8B8A8Unorm:
                case Format.R8G8B8A8Srgb:
                case Format.R8G8B8A8SNorm:
                case Format.R8G8B8A8Uint:
                case Format.R8G8B8A8Sint:
                case Format.B8G8R8A8Unorm:
                case Format.B8G8R8A8Srgb:
                case Format.A2R10G10B10UnormPack32:
                case Format.A2R10G10B10UintPack32:
                case Format.B10G11R11UfloatPack32:
                case Format.D24UnormS8Uint:
                    return 4;

                case Format.D32SfloatS8Uint:
                    return 5;

                case Format.R16G16B16A16Unorm:
                case Format.R16G16B16A16SNorm:
                case Format.R16G16B16A16Uint:
                case Format.R16G16B16A16Sint:
                case Format.R16G16B16A16Sfloat:
                case Format.R32G32Uint:
                case Format.R32G32Sint:
                case Format.R32G32Sfloat:
                    return 8;

                case Format.R32G32B32A32Sfloat:
                case Format.R32G32B32A32Uint:
                case Format.R32G32B32A32Sint:
                    return 16;
            }

            if (IsCompressedFormat(format))
            {
                Debug.Fail("GetSizeInBytes should not be used on a compressed format.");
            }

            throw new Exception();
        }

        internal static uint GetSampleCountUInt32(SampleCountFlags sampleCount)
        {
            switch (sampleCount)
            {
                case SampleCountFlags.SampleCount1Bit:
                    return 1;
                case SampleCountFlags.SampleCount2Bit:
                    return 2;
                case SampleCountFlags.SampleCount4Bit:
                    return 4;
                case SampleCountFlags.SampleCount8Bit:
                    return 8;
                case SampleCountFlags.SampleCount16Bit:
                    return 16;
                case SampleCountFlags.SampleCount32Bit:
                    return 32;
                case SampleCountFlags.SampleCount64Bit:
                    return 64;
                default:
                    throw new Exception();
            }
        }

        internal static bool IsStencilFormat(Format format)
        {
            return format == Format.D24UnormS8Uint || format == Format.D32SfloatS8Uint;
        }

        internal static bool IsDepthStencilFormat(Format format)
        {
            return format == Format.D32SfloatS8Uint
                   || format == Format.D24UnormS8Uint
                   || format == Format.R16Unorm
                   || format == Format.R32Sfloat;
        }

        internal static bool IsCompressedFormat(Format format)
        {
            return format == Format.BC1RgbaUnormBlock
                   || format == Format.BC1RgbaSrgbBlock
                   || format == Format.BC1RgbUnormBlock
                   || format == Format.BC1RgbSrgbBlock
                   || format == Format.BC2SrgbBlock
                   || format == Format.BC2UnormBlock
                   || format == Format.BC3SrgbBlock
                   || format == Format.BC3UnormBlock
                   || format == Format.BC4UnormBlock
                   || format == Format.BC4SNormBlock
                   || format == Format.BC5UnormBlock
                   || format == Format.BC5SNormBlock
                   || format == Format.BC7SrgbBlock
                   || format == Format.BC7UnormBlock
                   || format == Format.Etc2R8G8B8UnormBlock
                   || format == Format.Etc2R8G8B8A1SrgbBlock
                   || format == Format.Etc2R8G8B8A1UnormBlock;
        }

        internal static uint GetRowPitch(uint width, Format format)
        {
            if (IsCompressedFormat(format))
            {
                var blocksPerRow = (width + 3) / 4;
                var blockSizeInBytes = GetBlockSizeInBytes(format);
                return blocksPerRow * blockSizeInBytes;
            }

            return width * GetSizeInBytes(format);
        }

        public static uint GetBlockSizeInBytes(Format format)
        {
            switch (format)
            {
                case Format.BC1RgbaSrgbBlock:
                case Format.BC1RgbaUnormBlock:
                case Format.BC1RgbSrgbBlock:
                case Format.BC1RgbUnormBlock:
                case Format.BC4UnormBlock:
                case Format.BC4SNormBlock:
                case Format.Etc2R8G8B8UnormBlock:
                case Format.Etc2R8G8B8A1UnormBlock:
                    return 8;
                case Format.BC2SrgbBlock:
                case Format.BC2UnormBlock:
                case Format.BC3SrgbBlock:
                case Format.BC3UnormBlock:
                case Format.BC5UnormBlock:
                case Format.BC5SNormBlock:
                case Format.BC7SrgbBlock:
                case Format.BC7UnormBlock:
                case Format.Etc2R8G8B8A8UnormBlock:
                    return 16;
                default:
                    throw new Exception();
            }
        }

        internal static uint GetNumRows(uint height, Format format)
        {
            switch (format)
            {
                case Format.BC1RgbaSrgbBlock:
                case Format.BC1RgbaUnormBlock:
                case Format.BC1RgbSrgbBlock:
                case Format.BC1RgbUnormBlock:
                case Format.BC2SrgbBlock:
                case Format.BC2UnormBlock:
                case Format.BC3SrgbBlock:
                case Format.BC3UnormBlock:
                case Format.BC4UnormBlock:
                case Format.BC4SNormBlock:
                case Format.BC5UnormBlock:
                case Format.BC5SNormBlock:
                case Format.BC7SrgbBlock:
                case Format.BC7UnormBlock:
                case Format.Etc2R8G8B8UnormBlock:
                case Format.Etc2R8G8B8A1UnormBlock:
                case Format.Etc2R8G8B8A8UnormBlock:
                    return (height + 3) / 4;

                default:
                    return height;
            }
        }

        internal static uint GetDepthPitch(uint rowPitch, uint height, Format format)
        {
            return rowPitch * GetNumRows(height, format);
        }

        internal static uint GetRegionSize(uint width, uint height, uint depth, Format format)
        {
            uint blockSizeInBytes;
            if (IsCompressedFormat(format))
            {
                Debug.Assert((width % 4 == 0 || width < 4) && (height % 4 == 0 || height < 4));
                blockSizeInBytes = GetBlockSizeInBytes(format);
                width /= 4;
                height /= 4;
            }
            else
            {
                blockSizeInBytes = GetSizeInBytes(format);
            }

            return width * height * depth * blockSizeInBytes;
        }

        internal static SampleCountFlags GetSampleCount(uint samples)
        {
            switch (samples)
            {
                case 1: return SampleCountFlags.SampleCount1Bit;
                case 2: return SampleCountFlags.SampleCount2Bit;
                case 4: return SampleCountFlags.SampleCount4Bit;
                case 8: return SampleCountFlags.SampleCount8Bit;
                case 16: return SampleCountFlags.SampleCount16Bit;
                case 32: return SampleCountFlags.SampleCount32Bit;
                case 64: return SampleCountFlags.SampleCount64Bit;
                default: throw new MintyCoreException("Unsupported multisample count: " + samples);
            }
        }

        internal static Format GetViewFamilyFormat(Format format)
        {
            switch (format)
            {
                case Format.R32G32B32A32Sfloat:
                case Format.R32G32B32A32Uint:
                case Format.R32G32B32A32Sint:
                    return Format.R32G32B32A32Sfloat;
                case Format.R16G16B16A16Sfloat:
                case Format.R16G16B16A16Unorm:
                case Format.R16G16B16A16Uint:
                case Format.R16G16B16A16SNorm:
                case Format.R16G16B16A16Sint:
                    return Format.R16G16B16A16Sfloat;
                case Format.R32G32Sfloat:
                case Format.R32G32Uint:
                case Format.R32G32Sint:
                    return Format.R32G32Sfloat;
                case Format.A2R10G10B10UnormPack32:
                case Format.A2R10G10B10UintPack32:
                    return Format.A2R10G10B10UnormPack32;
                case Format.R8G8B8A8Unorm:
                case Format.R8G8B8A8Srgb:
                case Format.R8G8B8A8Uint:
                case Format.R8G8B8A8SNorm:
                case Format.R8G8B8A8Sint:
                    return Format.R8G8B8A8Unorm;
                case Format.R16G16Sfloat:
                case Format.R16G16Unorm:
                case Format.R16G16Uint:
                case Format.R16G16SNorm:
                case Format.R16G16Sint:
                    return Format.R16G16Sfloat;
                case Format.R32Sfloat:
                case Format.R32Uint:
                case Format.R32Sint:
                    return Format.R32Sfloat;
                case Format.R8G8Unorm:
                case Format.R8G8Uint:
                case Format.R8G8SNorm:
                case Format.R8G8Sint:
                    return Format.R8G8Unorm;
                case Format.R16Sfloat:
                case Format.R16Unorm:
                case Format.R16Uint:
                case Format.R16SNorm:
                case Format.R16Sint:
                    return Format.R16Sfloat;
                case Format.R8Unorm:
                case Format.R8Uint:
                case Format.R8SNorm:
                case Format.R8Sint:
                    return Format.R8Unorm;
                case Format.BC1RgbaUnormBlock:
                case Format.BC1RgbaSrgbBlock:
                case Format.BC1RgbUnormBlock:
                case Format.BC1RgbSrgbBlock:
                    return Format.BC1RgbaUnormBlock;
                case Format.BC2UnormBlock:
                case Format.BC2SrgbBlock:
                    return Format.BC2UnormBlock;
                case Format.BC3UnormBlock:
                case Format.BC3SrgbBlock:
                    return Format.BC3UnormBlock;
                case Format.BC4UnormBlock:
                case Format.BC4SNormBlock:
                    return Format.BC4UnormBlock;
                case Format.BC5UnormBlock:
                case Format.BC5SNormBlock:
                    return Format.BC5UnormBlock;
                case Format.B8G8R8A8Unorm:
                case Format.B8G8R8A8Srgb:
                    return Format.B8G8R8A8Unorm;
                case Format.BC7UnormBlock:
                case Format.BC7SrgbBlock:
                    return Format.BC7UnormBlock;
                default:
                    return format;
            }
        }
    }
}