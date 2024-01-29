using System;
using System.Collections.Generic;
using MintyCore.Utils;

namespace MintyCore.Graphics.Render.Implementations;

[Singleton<IInputManager>(SingletonContextFlags.NoHeadless)]
internal class InputManager : IInputManager
{
    private readonly Dictionary<Identification, DictionaryInputData> _indexedInputData = new();
    private readonly Dictionary<Identification, SingletonInputData> _singletonInputData = new();
    
    private readonly HashSet<Identification> _registeredInputDataModules = new();

    public void RegisterKeyIndexedInputDataType(Identification id, DictionaryInputDataRegistryWrapper wrapper)
    {
        _indexedInputData.Add(id, wrapper.GetDictionaryInputData());
    }

    public void SetKeyIndexedInputData<TKey, TData>(Identification id, TKey key, TData data)
        where TKey : notnull
    {
        if (!_indexedInputData.TryGetValue(id, out var obj))
            throw new MintyCoreException($"No dictionary object found for {id}");

        if (obj is not DictionaryInputData<TKey, TData> dic)
            throw new MintyCoreException(
                $"Type mismatch for {id}. Expected <{obj.KeyType.FullName}, {obj.DataType.FullName}> but got <{typeof(TKey).FullName}, {typeof(TData).FullName}>");

        dic.SetData(key, data);
    }

    public void RegisterInputDataModule<TModule>(Identification id) where TModule : InputDataModule
    {
        if (!_registeredInputDataModules.Add(id))
            throw new MintyCoreException($"Input Data Module for {id} is already registered");

        _registeredInputDataModules.Add(id);
    }

    public SingletonInputData<TData> GetSingletonInputData<TData>(Identification inputDataId) where TData : notnull
    {
        if (!_singletonInputData.TryGetValue(inputDataId, out var inputData))
            throw new MintyCoreException($"Singleton Input Type for {inputDataId} is not registered");

        if (inputData is not SingletonInputData<TData> concreteInputData)
            throw new MintyCoreException(
                $"Wrong type for {inputDataId}, expected {inputData.DataType.FullName} but got {typeof(TData).FullName}");

        return concreteInputData;
    }

    public DictionaryInputData<TKey, TData> GetDictionaryInputData<TKey, TData>(Identification inputDataId)
        where TKey : notnull
    {
        if (!_indexedInputData.TryGetValue(inputDataId, out var inputData))
            throw new MintyCoreException($"Dictionary Input Type for {inputDataId} is not registered");

        if (inputData is not DictionaryInputData<TKey, TData> concreteInputData)
            throw new MintyCoreException(
                $"Wrong type for {inputDataId}, expected <{inputData.KeyType.FullName}, {inputData.DataType.FullName}> but got <{typeof(TKey).FullName}, {typeof(TData).FullName}>");

        return concreteInputData;
    }

    public void RegisterSingletonInputDataType(Identification id, SingletonInputDataRegistryWrapper wrapper)
    {
        _singletonInputData.Add(id, wrapper.GetSingletonInputData());
    }

    public void SetSingletonInputData<TDataType>(Identification id, TDataType data) where TDataType : notnull
    {
        if (!_singletonInputData.TryGetValue(id, out var inputData))
            throw new MintyCoreException($"Singleton Input Type for {id} is not registered");

        if (inputData is not SingletonInputData<TDataType> concreteInputData)
            throw new MintyCoreException(
                $"Wrong type for {id}, expected {inputData.DataType.FullName} but got {typeof(TDataType).FullName}");

        concreteInputData.SetData(data);
    }
}