using System;
using JetBrains.Annotations;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Render.Utils;

/// <summary>
///     Helper class for various vulkan functions
/// </summary>
[PublicAPI]
public static unsafe class VulkanUtils
{
    

    /// <summary>
    ///     Assert the vulkan result. Throws error if no success
    /// </summary>
    /// <param name="result">Result of a vulkan operation</param>
    /// <exception cref="VulkanException">result != <see cref="Result.Success" /></exception>
    public static void Assert(Result result)
    {
        Logger.AssertAndThrow(result == Result.Success, $"Vulkan Execution Failed:  {result}", "Render");
    }

    
}

/// <summary>
///     Exception for vulkan errors
/// </summary>
public class VulkanException : Exception
{
    /// <summary>
    /// </summary>
    /// <param name="result"></param>
    public VulkanException(Result result) : base($"A Vulkan Exception occured({result})")
    {
    }

    /// <summary>
    /// </summary>
    /// <param name="message"></param>
    public VulkanException(string message) : base(message)
    {
    }
}

/// <summary>
///     Struct containing queue family indexes
/// </summary>
public struct QueueFamilyIndexes
{
    /// <summary>
    ///     Index of graphics family
    /// </summary>
    public uint? GraphicsFamily;

    /// <summary>
    ///     Index of present family
    /// </summary>
    public uint? PresentFamily;

    /// <summary>
    ///     Index of compute family
    /// </summary>
    public uint? ComputeFamily;
}