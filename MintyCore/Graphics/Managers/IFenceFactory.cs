using MintyCore.Graphics.VulkanObjects;
using Silk.NET.Vulkan;

namespace MintyCore.Graphics.Managers;

public interface IFenceFactory
{
    public ManagedFence CreateFence(FenceCreateFlags flags = 0);
}