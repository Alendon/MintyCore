using System.Collections.Generic;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Modding;
using MintyCore.Modding.Attributes;
using MintyCore.Modding.Implementations;
using MintyCore.Utils;

namespace MintyCore.Registries;

[Registry("component_serializer")]
public class ComponentSerializerRegistry(IComponentManager componentManager) : IRegistry
{
    public ushort RegistryId => RegistryIDs.ComponentSerializer;
    public IEnumerable<ushort> RequiredRegistries => [RegistryIDs.Component];

    [RegisterMethod(ObjectRegistryPhase.Main)]
    public void RegisterComponentSerializer<TComponentSerializer>(Identification objectId)
        where TComponentSerializer : ComponentSerializer
    {
        componentManager.AddComponentSerializer<TComponentSerializer>(objectId);
    }

    public void UnRegister(Identification objectId)
    {
        componentManager.RemoveComponentSerializer(objectId);
    }

    public void PreUnRegister()
    {
        componentManager.DestroyComponentSerializers();
    }

    public void PostUnRegister()
    {
        componentManager.BuildComponentSerializers();
    }

    public void PreRegister(ObjectRegistryPhase currentPhase)
    {
        if (currentPhase == ObjectRegistryPhase.Main)
            componentManager.DestroyComponentSerializers();
    }

    public void PostRegister(ObjectRegistryPhase currentPhase)
    {
        if (currentPhase == ObjectRegistryPhase.Main)
            componentManager.BuildComponentSerializers();
    }

    public void Clear()
    {
    }
}