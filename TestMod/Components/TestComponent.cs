using MintyCore.ECS;
using MintyCore.Registries;
using MintyCore.Utils;
using TestMod.Identifications;

namespace TestMod.Components;

[RegisterComponent("test")]
public struct TestComponent : IComponent
{
    public bool Dirty
    {
        get => true;
        set
        {
            
        }
    }

    public Identification Identification => ComponentIDs.Test;
    
    public void PopulateWithDefaultValues()
    {
    }

    public void Serialize(DataWriter writer, IWorld world, Entity entity)
    {
    }

    public bool Deserialize(DataReader reader, IWorld world, Entity entity)
    {
        throw new NotSupportedException();
    }

    public void IncreaseRefCount()
    {
    }

    public void DecreaseRefCount()
    {
    }
}