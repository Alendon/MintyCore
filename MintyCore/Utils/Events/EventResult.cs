namespace MintyCore.Utils.Events;

/// <summary>
/// Represents the result of an event in the event bus system.
/// </summary>
public enum EventResult
{
    /// <summary>
    /// Indicates that the event processing should continue.
    /// If an event handler returns this, the event bus will proceed to the next registered handler for the event.
    /// </summary>
    Continue,

    /// <summary>
    /// Indicates that the event processing should stop.
    /// If an event handler returns this, the event bus will not call any further registered handlers for the event.
    /// </summary>
    Stop
}