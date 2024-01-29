using JetBrains.Annotations;
using MintyCore.Utils;

namespace MintyCore.Graphics.Render;

/// <summary>
/// Interface for managing render input data
/// </summary>
[PublicAPI]
public interface IInputManager
{
    /// <summary>
    /// Set the current data for the given id
    /// </summary>
    /// <param name="id"> The id of the data </param>
    /// <param name="data"> The data to set </param>
    /// <typeparam name="TData"> The type of the data </typeparam>
    public void SetSingletonInputData<TData>(Identification id, TData data) where TData : notnull;

    /// <summary>
    ///  Set the current data for the given key for the given id
    /// </summary>
    /// <param name="id"> The id of the data </param>
    /// <param name="key"> The key of the data to set </param>
    /// <param name="data"> The data to set </param>
    /// <typeparam name="TKey"> The type of the key </typeparam>
    /// <typeparam name="TData"> The type of the data </typeparam>
    public void SetKeyIndexedInputData<TKey, TData>(Identification id, TKey key, TData data)
        where TKey : notnull;

    /// <summary>
    /// Registers an input data module with the given id
    /// </summary>
    /// <param name="id"> The id of the module </param>
    /// <typeparam name="TModule"> The type of the module </typeparam>
    /// <remarks>Not intended to be called by user code</remarks>
    public void RegisterInputDataModule<TModule>(Identification id) where TModule : InputDataModule;

    /// <summary>
    ///  Get the current data for the given id
    /// </summary>
    /// <param name="inputDataId"> The id of the data </param>
    /// <typeparam name="TData"> The type of the data </typeparam>
    /// <returns> The current data </returns>
    public SingletonInputData<TData> GetSingletonInputData<TData>(Identification inputDataId)
        where TData : notnull;
    
    /// <summary>
    ///  Get the current data for the given key for the given id
    /// </summary>
    /// <param name="inputDataId"> The id of the data </param>
    /// <typeparam name="TKey"> The type of the key </typeparam>
    /// <typeparam name="TData"> The type of the data </typeparam>
    /// <returns> The current data </returns>
    public DictionaryInputData<TKey, TData> GetDictionaryInputData<TKey, TData>(Identification inputDataId)
        where TKey : notnull;

    /// <summary>
    /// Register a singleton input data type
    /// </summary>
    /// <param name="id"> The id of the data </param>
    /// <param name="wrapper"> The wrapper for the data </param>
    /// <remarks> Not intended to be called by user code </remarks>
    void RegisterSingletonInputDataType(Identification id, SingletonInputDataRegistryWrapper wrapper);
    
    /// <summary>
    ///  Register a key indexed input data type
    /// </summary>
    /// <param name="id"> The id of the data </param>
    /// <param name="wrapper"> The wrapper for the data </param>
    /// <remarks> Not intended to be called by user code </remarks>
    void RegisterKeyIndexedInputDataType(Identification id, DictionaryInputDataRegistryWrapper wrapper);
    
    
}