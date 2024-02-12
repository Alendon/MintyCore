using MintyCore.Graphics.VulkanObjects;

namespace MintyCore.Graphics.Managers;

/// <summary>
///    Factory for creating <see cref="ManagedCommandPool" /> instances
/// </summary>
public interface ICommandPoolFactory
{
    /// <summary>
    ///   Create a new <see cref="ManagedCommandPool" /> instance
    /// </summary>
    public ManagedCommandPool CreateCommandPool(CommandPoolDescription description);
}

/// <summary>
///  Description of a <see cref="ManagedCommandPool" /> to be created
/// </summary>
/// <param name="QueueFamilyIndex"> The index of the queue family to create the pool for </param>
/// <param name="IsResettable"> Whether individual command buffers can be reset </param>
/// <param name="IsTransient"></param>
/// <param name="IsProtected"></param>
public record CommandPoolDescription(
    uint QueueFamilyIndex,
    bool IsResettable = false,
    bool IsTransient = false,
    bool IsProtected = false);