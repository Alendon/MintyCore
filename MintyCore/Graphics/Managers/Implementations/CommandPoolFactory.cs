using MintyCore.Graphics.Utils;
using MintyCore.Graphics.VulkanObjects;
using MintyCore.Utils;
using Silk.NET.Vulkan;

namespace MintyCore.Graphics.Managers.Implementations;

[Singleton<ICommandPoolFactory>(SingletonContextFlags.NoHeadless)]
internal class CommandPoolFactory(IVulkanEngine vulkanEngine, IAllocationHandler allocationHandler) : ICommandPoolFactory
{
    public unsafe ManagedCommandPool CreateCommandPool(CommandPoolDescription description)
    {
        CommandPoolCreateFlags flags = 0;
        if (description.IsResettable)
            flags |= CommandPoolCreateFlags.ResetCommandBufferBit;
        if (description.IsTransient)
            flags |= CommandPoolCreateFlags.TransientBit;
        if (description.IsProtected)
            flags |= CommandPoolCreateFlags.ProtectedBit;

        var createInfo = new CommandPoolCreateInfo()
        {
            SType = StructureType.CommandPoolCreateInfo,
            QueueFamilyIndex = description.QueueFamilyIndex,
            Flags = flags
        };
        
        VulkanUtils.Assert(vulkanEngine.Vk.CreateCommandPool(vulkanEngine.Device, createInfo, null, out var commandPool));
        
        return new ManagedCommandPool(vulkanEngine, allocationHandler, commandPool, description);
    }
}