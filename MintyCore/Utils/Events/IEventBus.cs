using JetBrains.Annotations;

namespace MintyCore.Utils.Events;

/// <summary>
/// Interface for the global event bus.
/// </summary>
[PublicAPI]
public interface IEventBus
{
    /// <summary>
    /// Register an event type with the event bus.
    /// </summary>
    /// <remarks>Do not call this method directly. Use the <see cref="MintyCore.Registries.RegisterEventAttribute"/></remarks>
    void RegisterEventType<TEvent>(Identification id) where TEvent : struct, IEvent;

    /// <summary>
    /// Remove an event type from the event bus.
    /// </summary>
    /// <remarks> Do not call this method directly. Use the <see cref="MintyCore.Registries.RegisterEventAttribute"/></remarks>
    void RemoveEventType(Identification id);

    /// <summary>
    /// Add a listener to the event bus, listening for the specified event.
    /// </summary>
    /// <param name="binding"> The binding to add. </param>
    /// <typeparam name="TEvent"> The event type to listen for. </typeparam>
    void AddListener<TEvent>(EventBinding<TEvent> binding) where TEvent : struct, IEvent;

    /// <summary>
    ///  Remove a listener from the event bus.
    /// </summary>
    /// <param name="binding"> The binding to remove. </param>
    /// <typeparam name="TEvent"> The event type to remove the listener for. </typeparam>
    void RemoveListener<TEvent>(EventBinding<TEvent> binding) where TEvent : struct, IEvent;

    /// <summary>
    ///  Sort all event listeners
    /// </summary>
    void SortListeners();

    /// <summary>
    /// Invoke an event on the event bus.
    /// </summary>
    /// <param name="e"> The event to invoke. </param>
    /// <typeparam name="TEvent"> The type of event to invoke. </typeparam>
    /// <returns> The event after it has been processed by all listeners. The data may have been modified. </returns>
    TEvent InvokeEvent<TEvent>(TEvent e) where TEvent : struct, IEvent;
}