using MintyCore.Graphics.VulkanObjects;

namespace MintyCore.Graphics.Managers;

public interface ICommandPoolFactory
{
    public ManagedCommandPool CreateCommandPool(CommandPoolDescription description);
}

public record CommandPoolDescription(
    uint QueueFamilyIndex,
    bool IsResettable = false,
    bool IsTransient = false,
    bool IsProtected = false);