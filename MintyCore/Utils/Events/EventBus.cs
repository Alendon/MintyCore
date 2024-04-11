using System;
using System.Collections.Generic;

namespace MintyCore.Utils.Events;

[Singleton<IEventBus>]
internal class EventBus : IEventBus
{
    private readonly Dictionary<Identification, List<EventBinding>> _eventBindings = new();
    
    
    public void RegisterEventType<TEvent>(Identification id) where TEvent : struct, IEvent
    {
        var eventId = TEvent.Identification;
        if (eventId != id)
        {
            throw new ArgumentException($"Event Id stored in {typeof(TEvent).Name} does not match the provided id. Expected {eventId} but got {id}.");
        }
        
        _eventBindings.Add(id, new List<EventBinding>());
    }

    public void RemoveEventType(Identification id)
    {
        _eventBindings.Remove(id);
    }

    public void AddListener<TEvent>(EventBinding<TEvent> binding) where TEvent : struct, IEvent
    {
        var id = TEvent.Identification;
        
        if (!_eventBindings.TryGetValue(id, out var bindings))
        {
            throw new ArgumentException($"Event Id {id} is not registered.");
        }

        bindings.Add(binding);
    }

    public void RemoveListener<TEvent>(EventBinding<TEvent> binding) where TEvent : struct, IEvent
    {
        var id = TEvent.Identification;
        
        if (!_eventBindings.TryGetValue(id, out var bindings))
        {
            throw new ArgumentException($"Event Id {id} is not registered.");
        }

        bindings.Remove(binding);
    }
    
    public void SortListeners(Identification id)
    {
        if (!_eventBindings.ContainsKey(id))
        {
            throw new ArgumentException($"Event Id {id} is not registered.");
        }
        
        _eventBindings[id].Sort((a, b) => b.Priority.CompareTo(a.Priority));
    }
    
    public TEvent InvokeEvent<TEvent>(TEvent e) where TEvent : struct, IEvent
    {
        var id = TEvent.Identification;
        
        if (!_eventBindings.TryGetValue(id, out var bindings))
        {
            throw new ArgumentException($"Event Id {id} is not registered.");
        }

        foreach (var binding in bindings)
        {
            if(binding is not EventBinding<TEvent> actualBinding)
            {
                throw new InvalidCastException($"Binding for event {id} is not of the correct type.");
            }

            var res = actualBinding.Invoke(ref e);

            if (res is EventResult.Stop) break;
        }
        
        return e;
    }
}