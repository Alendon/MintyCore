using System;
using JetBrains.Annotations;
using MintyCore.Graphics.Render.Data;
using MintyCore.Utils;

namespace MintyCore.Graphics.Render;

[PublicAPI]
public interface IInputModuleDataAccessor
{
    SingletonInputData<TInputData> UseSingletonInputData<TInputData>(Identification inputDataId,
        InputModule inputModule) where TInputData : notnull;

    DictionaryInputData<TKey, TData> UseDictionaryInputData<TKey, TData>(Identification inputDataId,
        InputModule inputModule) where TKey : notnull;

    Func<TIntermediateData> UseIntermediateData<TIntermediateData>(Identification intermediateDataId,
        InputModule inputModule) where TIntermediateData : IntermediateData;

    Func<TIntermediateData> ProvideIntermediateData<TIntermediateData>(Identification intermediateDataId,
        InputModule inputModule) where TIntermediateData : IntermediateData;
}