using JetBrains.Annotations;
using MintyCore.Utils;

namespace MintyCore.Graphics.Render;

[PublicAPI]
public interface IInputManager
{
    public void SetSingletonInputData<TData>(Identification id, TData data) where TData : notnull;

    public void SetKeyIndexedInputData<TKey, TData>(Identification id, TKey key, TData data)
        where TKey : notnull;

    public void RegisterInputDataModule<TModule>(Identification id) where TModule : InputDataModule;

    public SingletonInputData<TData> GetSingletonInputData<TData>(Identification inputDataId)
        where TData : notnull;
    
    public DictionaryInputData<TKey, TData> GetDictionaryInputData<TKey, TData>(Identification inputDataId)
        where TKey : notnull;

    void RegisterSingletonInputDataType(Identification id, SingletonInputDataRegistryWrapper wrapper);
    void RegisterKeyIndexedInputDataType(Identification id, DictionaryInputDataRegistryWrapper wrapper);
    
    
}