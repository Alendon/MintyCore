namespace MintyCore.Utils.Events;

public interface IEventBus
{
    void RegisterEventType<TEvent>(Identification id) where TEvent : struct, IEvent;
    void RemoveEventType(Identification id);
    
    void AddListener<TEvent>(EventBinding<TEvent> binding) where TEvent : struct, IEvent;
    void RemoveListener<TEvent>(EventBinding<TEvent> binding) where TEvent : struct, IEvent;
    void SortListeners(Identification id);

}