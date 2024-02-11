using JetBrains.Annotations;
using MintyCore.Graphics.Utils;
using MintyCore.Graphics.VulkanObjects;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Graphics.Managers.Implementations;

[Singleton<IFenceFactory>(SingletonContextFlags.NoHeadless)]
internal class FenceFactory : IFenceFactory
{
    public IVulkanEngine VulkanEngine { private get; [UsedImplicitly] set; } = null!;
    public IAllocationHandler AllocationHandler { private get; [UsedImplicitly] set; } = null!;
    
    public unsafe ManagedFence CreateFence(FenceCreateFlags flags = FenceCreateFlags.None)
    {
        var createInfo = new FenceCreateInfo
        {
            SType = StructureType.FenceCreateInfo,
            Flags = flags
        };
        
        VulkanUtils.Assert(VulkanEngine.Vk.CreateFence(VulkanEngine.Device, createInfo, null, out var fence));
        
        return new ManagedFence(VulkanEngine, AllocationHandler, fence);
    }
}