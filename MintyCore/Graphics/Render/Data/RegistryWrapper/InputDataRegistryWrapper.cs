namespace MintyCore.Graphics.Render.Data.RegistryWrapper;

public abstract class SingletonInputDataRegistryWrapper
{
    public abstract SingletonInputData GetSingletonInputData();
}

public class SingletonInputDataRegistryWrapper<TData> : SingletonInputDataRegistryWrapper where TData : notnull
{
    public override SingletonInputData GetSingletonInputData()
    {
        return new SingletonInputData<TData>();
    }
}

public abstract class DictionaryInputDataRegistryWrapper
{
    public abstract DictionaryInputData GetDictionaryInputData();
}

public class DictionaryInputDataRegistryWrapper<TKey, TData> : DictionaryInputDataRegistryWrapper where TKey : notnull
{
    public override DictionaryInputData GetDictionaryInputData()
    {
        return new DictionaryInputData<TKey, TData>();
    }
}