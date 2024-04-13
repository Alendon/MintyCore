using System;
using JetBrains.Annotations;

namespace MintyCore.Utils.Events;

/// <summary>
/// Base class for event bindings.
/// </summary>
[PublicAPI]
public abstract class EventBinding
{
    /// <summary>
    /// Gets or sets whether the binding is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>
    /// Gets whether the binding is a reference binding.
    /// If true, the event data can be modified by the callback.
    /// </summary>
    public abstract bool IsRefBinding { get; }
    
    public EventPriority Priority { get; init; } = EventPriority.Normal;
}

/// <summary>
///  Event binding for a specific event type.
/// </summary>
/// <typeparam name="TEvent"> Type of event to bind to. </typeparam>
[PublicAPI]
public class EventBinding<TEvent> : EventBinding where TEvent : struct, IEvent
{
    private readonly EventCallback? _callback;
    private readonly RefEventCallback? _refCallback;

    /// <inheritdoc />
    public override bool IsRefBinding => _refCallback is not null;

    /// <summary>
    /// Represents a callback for an event.
    /// </summary>
    /// <param name="e">The event data.</param>
    /// <returns>The result of the event.</returns>
    public delegate EventResult EventCallback(TEvent e);
    
    /// <summary>
    /// Represents a callback for an event that can be modified.
    /// </summary>
    /// <param name="e">The modifiable event data.</param>
    /// <returns>The result of the event.</returns>
    public delegate EventResult RefEventCallback(ref TEvent e);

    public EventBinding(EventCallback callback)
    {
        _callback = callback;
    }

    public EventBinding(IEventBus eventBus, EventCallback callback) : this(callback)
    {
        RegisterBinding(eventBus);
    }

    public EventBinding(RefEventCallback callback)
    {
        if (!TEvent.ModificationAllowed)
        {
            throw new InvalidOperationException($"Event {typeof(TEvent).Name} does not allow modification.");
        }
        
        _refCallback = callback;
    }
    
    public EventBinding(IEventBus eventBus, RefEventCallback callback) : this(callback)
    {
        RegisterBinding(eventBus);
    }

    public void RegisterBinding(IEventBus eventBus)
    {
        eventBus.AddListener(this);
    }
    
    public void UnregisterBinding(IEventBus eventBus)
    {
        eventBus.RemoveListener(this);
    }

    public EventResult Invoke(ref TEvent e)
    {
        if (!IsEnabled) return EventResult.Continue;
        
        return IsRefBinding switch {
            true => _refCallback!(ref e),
            false => _callback!(e)
        };
    }

}