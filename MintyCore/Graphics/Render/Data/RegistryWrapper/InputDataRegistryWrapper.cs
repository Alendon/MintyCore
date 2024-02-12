namespace MintyCore.Graphics.Render.Data.RegistryWrapper;

/// <summary>
/// Abstract class for singleton input data registry wrapper.
/// </summary>
public abstract class SingletonInputDataRegistryWrapper
{
    /// <summary>
    /// Creates a new instance of singleton input data.
    /// </summary>
    /// <returns>A new instance of singleton input data.</returns>
    public abstract SingletonInputData NewSingletonInputData();
}

/// <summary>
/// Class for singleton input data registry wrapper with specific data type.
/// </summary>
/// <typeparam name="TData">The type of the data.</typeparam>
public class SingletonInputDataRegistryWrapper<TData> : SingletonInputDataRegistryWrapper where TData : notnull
{
    /// <summary>
    /// Creates a new instance of singleton input data with specific data type.
    /// </summary>
    /// <returns>A new instance of singleton input data with specific data type.</returns>
    public override SingletonInputData NewSingletonInputData()
    {
        return new SingletonInputData<TData>();
    }
}

/// <summary>
/// Abstract class for dictionary input data registry wrapper.
/// </summary>
public abstract class DictionaryInputDataRegistryWrapper
{
    /// <summary>
    /// Creates a new instance of dictionary input data.
    /// </summary>
    /// <returns>A new instance of dictionary input data.</returns>
    public abstract DictionaryInputData NewDictionaryInputData();
}

/// <summary>
/// Class for dictionary input data registry wrapper with specific key and data types.
/// </summary>
/// <typeparam name="TKey">The type of the key.</typeparam>
/// <typeparam name="TData">The type of the data.</typeparam>
public class DictionaryInputDataRegistryWrapper<TKey, TData> : DictionaryInputDataRegistryWrapper where TKey : notnull
{
    /// <summary>
    /// Creates a new instance of dictionary input data with specific key and data types.
    /// </summary>
    /// <returns>A new instance of dictionary input data with specific key and data types.</returns>
    public override DictionaryInputData NewDictionaryInputData()
    {
        return new DictionaryInputData<TKey, TData>();
    }
}