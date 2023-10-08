using System;
using System.Diagnostics;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Render.Utils;

/// <summary>
///     Helper class to work with <see cref="Format" />
/// </summary>
public static class FormatHelpers
{
    /// <summary>
    ///     Get the size of a format in bytes
    /// </summary>
    /// <param name="format"></param>
    /// <returns></returns>
    /// <exception cref="Exception">A compressed format is passed</exception>
    public static uint GetSizeInBytes(Format format)
    {
        // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
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

        if (IsCompressedFormat(format)) Debug.Fail("GetSizeInBytes should not be used on a compressed format.");

        throw new Exception();
    }

    /// <summary>
    ///     Convert the sample count flag to a uint value
    /// </summary>
    /// <param name="sampleCount"></param>
    /// <returns></returns>
    /// <exception cref="Exception">An invalid sample count is passed</exception>
    public static uint GetSampleCountUInt32(SampleCountFlags sampleCount)
    {
        return sampleCount switch
        {
            SampleCountFlags.Count1Bit => 1,
            SampleCountFlags.Count2Bit => 2,
            SampleCountFlags.Count4Bit => 4,
            SampleCountFlags.Count8Bit => 8,
            SampleCountFlags.Count16Bit => 16,
            SampleCountFlags.Count32Bit => 32,
            SampleCountFlags.Count64Bit => 64,
            _ => throw new Exception()
        };
    }

    /// <summary>
    ///     Check whether or not the passed format is a stencil format
    /// </summary>
    /// <param name="format">The format to check</param>
    /// <returns>True if stencil format</returns>
    public static bool IsStencilFormat(Format format)
    {
        return format is Format.D24UnormS8Uint or Format.D32SfloatS8Uint;
    }

    /// <summary>
    ///     Check whether or not the passed format is a depth stencil format
    /// </summary>
    /// <param name="format">The format to check</param>
    /// <returns>True if depth stencil format</returns>
    public static bool IsDepthStencilFormat(Format format)
    {
        return format is Format.D32SfloatS8Uint or Format.D24UnormS8Uint or Format.R16Unorm or Format.R32Sfloat;
    }

    /// <summary>
    ///     Check whether or not the format is a compressed format
    /// </summary>
    /// <param name="format">The format to check</param>
    /// <returns>True if compressed</returns>
    public static bool IsCompressedFormat(Format format)
    {
        return format is Format.BC1RgbaUnormBlock or Format.BC1RgbaSrgbBlock or Format.BC1RgbUnormBlock
            or Format.BC1RgbSrgbBlock or Format.BC2SrgbBlock or Format.BC2UnormBlock or Format.BC3SrgbBlock
            or Format.BC3UnormBlock or Format.BC4UnormBlock or Format.BC4SNormBlock or Format.BC5UnormBlock
            or Format.BC5SNormBlock or Format.BC7SrgbBlock or Format.BC7UnormBlock or Format.Etc2R8G8B8UnormBlock
            or Format.Etc2R8G8B8A1SrgbBlock or Format.Etc2R8G8B8A1UnormBlock;
    }

    /// <summary>
    ///     Calculate the row pitch
    /// </summary>
    /// <param name="width">The width of the row</param>
    /// <param name="format">The format in which the rows are stored</param>
    /// <returns>The calculated pitch of the row</returns>
    public static uint GetRowPitch(uint width, Format format)
    {
        if (!IsCompressedFormat(format)) return width * GetSizeInBytes(format);


        var blocksPerRow = (width + 3) / 4;
        var blockSizeInBytes = GetBlockSizeInBytes(format);
        return blocksPerRow * blockSizeInBytes;
    }

    /// <summary>
    ///     Get the block size of a format
    /// </summary>
    /// <param name="format"></param>
    /// <returns>Block size of the format in bytes</returns>
    /// <exception cref="Exception">Not a block format</exception>
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

    /// <summary>
    ///     Get the numbers of rows (will return height as rows if not a compressed format)
    /// </summary>
    /// <param name="height">The height</param>
    /// <param name="format">The format</param>
    /// <returns>The number of rows used</returns>
    public static uint GetNumRows(uint height, Format format)
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

    /// <summary>
    ///     Calculate the depth pitch
    /// </summary>
    /// <param name="rowPitch"></param>
    /// <param name="height"></param>
    /// <param name="format"></param>
    /// <returns></returns>
    public static uint GetDepthPitch(uint rowPitch, uint height, Format format)
    {
        return rowPitch * GetNumRows(height, format);
    }

    /// <summary>
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="depth"></param>
    /// <param name="format"></param>
    /// <returns></returns>
    public static uint GetRegionSize(uint width, uint height, uint depth, Format format)
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

    /// <summary>
    /// </summary>
    /// <param name="samples"></param>
    /// <returns></returns>
    /// <exception cref="MintyCoreException"></exception>
    public static SampleCountFlags GetSampleCount(uint samples)
    {
        switch (samples)
        {
            case 1: return SampleCountFlags.Count1Bit;
            case 2: return SampleCountFlags.Count2Bit;
            case 4: return SampleCountFlags.Count4Bit;
            case 8: return SampleCountFlags.Count8Bit;
            case 16: return SampleCountFlags.Count16Bit;
            case 32: return SampleCountFlags.Count32Bit;
            case 64: return SampleCountFlags.Count64Bit;
            default: throw new MintyCoreException("Unsupported multi sample count: " + samples);
        }
    }

    /// <summary>
    /// </summary>
    /// <param name="format"></param>
    /// <returns></returns>
    public static Format GetViewFamilyFormat(Format format)
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