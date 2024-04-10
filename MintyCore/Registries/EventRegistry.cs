using System.Collections.Generic;
using System.Linq;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Modding.Attributes;
using MintyCore.Modding.Implementations;
using MintyCore.Utils;
using MintyCore.Utils.Events;

namespace MintyCore.Registries;

[Registry("event")]
public class EventRegistry(IEventBus eventBus) : IRegistry
{
    ushort IRegistry.RegistryId => RegistryIDs.Event;

    /// <inheritdoc />
    public IEnumerable<ushort> RequiredRegistries => Enumerable.Empty<ushort>();
    
    [RegisterMethod(ObjectRegistryPhase.Main)]
    public void RegisterEvent<TEvent>(Identification id) where TEvent : struct, IEvent
    {
        eventBus.RegisterEventType<TEvent>(id);
    }

    /// <inheritdoc />
    public void UnRegister(Identification objectId)
    {
        eventBus.RemoveEventType(objectId);
    }

    /// <inheritdoc />
    public void Clear()
    {
        
    }
}