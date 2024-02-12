using MintyCore.Graphics.VulkanObjects;
using Silk.NET.Vulkan;

namespace MintyCore.Graphics.Managers;

/// <summary>
/// Manages the creation of vulkan fences
/// </summary>
public interface IFenceFactory
{
    /// <summary>
    ///   Create a new fence
    /// </summary>
    /// <param name="flags"> Flags for the fence</param>
    /// <returns> The created fence</returns>
    public ManagedFence CreateFence(FenceCreateFlags flags = 0);
}