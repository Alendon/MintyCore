using JetBrains.Annotations;

namespace MintyCore.Utils.Events;

/// <summary>
/// Define the priority of an event binding.
/// </summary>
[PublicAPI]
public enum BindingPriority
{
    /// <summary>
    ///  The lowest priority.
    /// </summary>
    Lowest = 0,

    /// <summary>
    ///  A low priority.
    /// </summary>
    Low = 1,

    /// <summary>
    ///  A normal priority.
    /// </summary>
    Normal = 2,

    /// <summary>
    ///  A high priority.
    /// </summary>
    High = 3,

    /// <summary>
    ///  The highest priority.
    /// </summary>
    Highest = 4
}