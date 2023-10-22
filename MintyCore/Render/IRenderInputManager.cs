using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MintyCore.Utils;

namespace MintyCore.Render;

[PublicAPI]
public interface IRenderInputManager : IDisposable
{
    void AddRenderInput<TRenderInput>(Identification renderInputId) where TRenderInput : IRenderInput;
    void RemoveRenderInput(Identification renderInputId);
    void ConstructRenderInputs();
    
    void SetData<TKey, TValue>(Identification renderInputId, TKey key, TValue value);
    void RemoveData<TKey>(Identification renderInputId, TKey key);
    Task ProcessAll();

    IRenderInput<TKey, TValue> GetRenderInput<TKey, TValue>(Identification renderInputId);
    IRenderInput<TKey> GetRenderInput<TKey>(Identification renderInputId);
    IRenderInput GetRenderInput(Identification renderInputId);
}