# Render Input Behavior

The @MintyCore.Render.IRenderInputManager is responsible for managing various input data required by the render modules.
The data is processed and prepared for rendering by the respective modules automatically when the calculation of a frame
starts.

## Interact with the Render Input Manager

To set the input data from outside, you need to use the @MintyCore.Render.IRenderInputManager.

To set a singular value, you can directly use the @MintyCore.Render.IRenderInputManager.SetData() method.

```csharp
IRenderInputManager renderInputManager = ...; //Access the IRenderInputManager through dependency injection.
Identification renderInputId = ...; //The identification of the render input plugin.
TKey key = ...; //The key of the data.
TValue value = ...; //The value of the data.
renderInputManager.SetData(renderInputId, key, value);
```

Or for multiple values, it is recommended to work directly with the appropriate @MintyCore.Render.IRenderInput object.

```csharp
IRenderInputManager renderInputManager = ...; //Access the IRenderInputManager through dependency injection.

Identification renderInputId = ...; //The identification of the render input plugin.
IRenderInput<TKey, TValue> renderInput = renderInputManager.GetRenderInput<TKey, TValue>(renderInputId);

Dictionary<TKey, TValue> data = ...; //The data to set.
foreach (KeyValuePair<TKey, TValue> dataEntry in data)
{
    renderInput.SetData(dataEntry.Key, dataEntry.Value);
}
```

## Create a Render Input Plugin

To create a new render input plugin, you need to create a class that implements the @MintyCore.Render.IRenderInput
interface.
Currently there are 3 different overloads to choose from. This will be changed in the future. It is recommended to use
the @MintyCore.Render.IRenderInput<TKey, TValue> interface.

```csharp
//In this example, there is a position attached to an entity.
[RegisterRenderInput("my_render_input")
public class MyRenderInput : IRenderInput<Entity, Vector3>
{
    private readonly Dictionary<Entity, Vector3> _data = new();
    
    public void SetData(Entity entity, Vector3 position)
    {
        //Store the position of the entity internally.
        _data[entity] = position;
    }
    
    public void RemoveData(Entity entity)
    {
        //Remove the position of the entity internally.
        _data.Remove(entity);
    }
    
    public Task Process()
    {
        //Do whatever is needed with the positions.
        //For example construct a buffer with them.
    }
}
```

The important rule about render input plugins is that, only in the @MintyCore.Render.IRenderInput.Process() method, they
should actually interact with the gpu. The Set and Remove methods should only store the data internally.