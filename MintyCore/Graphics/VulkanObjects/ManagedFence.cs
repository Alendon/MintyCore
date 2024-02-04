using System;
using JetBrains.Annotations;
using MintyCore.Graphics.Utils;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Graphics.VulkanObjects;

/// <summary>
///   A managed wrapper around a vulkan <see cref="Silk.NET.Vulkan.Fence" />
/// </summary>
[PublicAPI]
public sealed unsafe class ManagedFence : VulkanObject
{
    /// <summary>
    /// The internal vulkan <see cref="Silk.NET.Vulkan.Fence" />. This should normally not be used directly
    /// </summary>
    public Fence Fence { get; }

    /// <summary>
    /// Create a new <see cref="ManagedFence"/> with an already created native vulkan <see cref="Silk.NET.Vulkan.Fence"/>
    /// </summary>
    /// <param name="fence"></param>
    public ManagedFence(IVulkanEngine vulkanEngine, Fence fence) : base(vulkanEngine)
    {
        Fence = fence;
    }

    /// <summary>
    /// Create a new <see cref="ManagedFence"/>
    /// </summary>
    /// <param name="fenceCreateFlags">Flag describing the initial fence behaviour</param>
    public ManagedFence(IVulkanEngine vulkanEngine, IAllocationHandler allocationHandler,
        Fence fence) : base(vulkanEngine, allocationHandler)
    {
        Fence = fence;
    }

    /// <summary>
    /// Check if the fence is signaled
    /// </summary>
    /// <returns>
    /// True if the fence is signaled, false if not
    /// </returns>
    public bool IsSignaled()
    {
        var statusResult = VulkanEngine.Vk.GetFenceStatus(VulkanEngine.Device, Fence);
        if (statusResult == Result.Success)
        {
            return true;
        }

        if (statusResult != Result.NotReady)
        {
            VulkanUtils.Assert(statusResult);
            //This will throw an exception, as the only other possible result is a device lost error
        }

        return false;
    }

    /// <summary>
    ///  Reset the fence to the unsignaled state
    /// </summary>
    public void Reset()
    {
        VulkanUtils.Assert(VulkanEngine.Vk.ResetFences(VulkanEngine.Device, 1, Fence));
    }

    /// <summary>
    /// Wait for the fence to be signaled
    /// </summary>
    /// <param name="timeout">The timeout in milliseconds. If the timeout is 0 the function will return immediately</param>
    /// <returns> True if the fence was signaled, false if the timeout was reached</returns>
    public bool Wait(uint timeout = uint.MaxValue)
    {
        var fence = Fence;

        //the timeout is in milliseconds, but the function expects nanoseconds
        var nanoTimeOut = timeout * 1000000Ul;
        var result = VulkanEngine.Vk.WaitForFences(VulkanEngine.Device, 1, fence, true, nanoTimeOut);

        if (result == Result.Success)
        {
            return true;
        }

        if (result != Result.Timeout)
        {
            VulkanUtils.Assert(result);
            //This will throw an exception, as this is an error state
        }

        return false;
    }

    /// <summary>
    ///   Wait for multiple fences to be signaled
    /// </summary>
    /// <param name="fences"> The fences to wait for</param>
    /// <param name="waitAll"> If true, the function will only return if all fences are signaled. If false, the function will return if any fence is signaled</param>
    /// <param name="timeout"> The timeout in milliseconds. If the timeout is 0 the function will return immediately</param>
    /// <returns> True if the fence was signaled, false if the timeout was reached</returns>
    public static bool WaitMany(ManagedFence[] fences, bool waitAll, uint timeout = uint.MaxValue)
    {
        if (fences.Length == 0) return true;

        var vulkanEngine = fences[0].VulkanEngine;

        var fenceHandles = (stackalloc Fence[fences.Length]);
        for (var i = 0; i < fences.Length; i++)
        {
            fenceHandles[i] = fences[i].Fence;
        }

        //the timeout is in milliseconds, but the function expects nanoseconds
        var nanoTimeOut = timeout * 1_000_000Ul;
        var result = vulkanEngine.Vk.WaitForFences(vulkanEngine.Device, fenceHandles, waitAll, nanoTimeOut);

        if (result == Result.Success)
        {
            return true;
        }

        if (result != Result.Timeout)
        {
            VulkanUtils.Assert(result);
            //This will throw an exception, as this is an error state
        }

        return false;
    }

    protected override void ReleaseUnmanagedResources()
    {
        base.ReleaseUnmanagedResources();
        VulkanEngine.Vk.DestroyFence(VulkanEngine.Device, Fence, null);
    }

    /// <summary>
    /// Check if two <see cref="ManagedFence" />s are equal
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals(object? obj)
    {
        return obj is ManagedFence fence && Equals(fence);
    }

    /// <summary>
    ///  Check if two <see cref="ManagedFence" />s are equal
    /// </summary>
    public bool Equals(ManagedFence other)
    {
        return Fence.Handle == other.Fence.Handle;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(Fence.Handle);
    }
}