using System;
using JetBrains.Annotations;
using MintyCore.Render.Utils;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Render.VulkanObjects;

/// <summary>
///   A managed wrapper around a vulkan <see cref="Fence" />
/// </summary>
[PublicAPI]
public sealed unsafe class ManagedFence : VulkanObject
{
    /// <summary>
    /// The internal vulkan <see cref="Fence" />. This should normally not be used directly
    /// </summary>
    public Fence InternalFence { get; }
    
    /// <summary>
    /// Create a new <see cref="ManagedFence"/> with an already created native vulkan <see cref="Fence"/>
    /// </summary>
    /// <param name="internalFence"></param>
    public ManagedFence(IVulkanEngine vulkanEngine, Fence internalFence) : base(vulkanEngine)
    {
        InternalFence = internalFence;
    }

    /// <summary>
    /// Create a new <see cref="ManagedFence"/>
    /// </summary>
    /// <param name="fenceCreateFlags">Flag describing the initial fence behaviour</param>
    public ManagedFence(IVulkanEngine vulkanEngine, IAllocationHandler allocationHandler,
        FenceCreateFlags fenceCreateFlags = FenceCreateFlags.None) : base(vulkanEngine, allocationHandler)
    {
        var createInfo = new FenceCreateInfo
        {
            SType = StructureType.FenceCreateInfo,
            Flags = fenceCreateFlags
        };

        VulkanUtils.Assert(VulkanEngine.Vk.CreateFence(VulkanEngine.Device, createInfo, null,
            out var fence));

        InternalFence = fence;
    }

    /// <summary>
    /// Check if the fence is signaled
    /// </summary>
    /// <returns>
    /// True if the fence is signaled, false if not
    /// </returns>
    public bool IsSignaled()
    {
        var statusResult = VulkanEngine.Vk.GetFenceStatus(VulkanEngine.Device, InternalFence);
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
        VulkanUtils.Assert(VulkanEngine.Vk.ResetFences(VulkanEngine.Device, 1, InternalFence));
    }

    /// <summary>
    /// Wait for the fence to be signaled
    /// </summary>
    /// <param name="timeout">The timeout in milliseconds. If the timeout is 0 the function will return immediately</param>
    /// <returns> True if the fence was signaled, false if the timeout was reached</returns>
    public bool Wait(uint timeout = uint.MaxValue)
    {
        var fence = InternalFence;

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
            fenceHandles[i] = fences[i].InternalFence;
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
        VulkanEngine.Vk.DestroyFence(VulkanEngine.Device, InternalFence, null);
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
        return InternalFence.Handle == other.InternalFence.Handle;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(InternalFence.Handle);
    }
}