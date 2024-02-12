using System;
using JetBrains.Annotations;
using MintyCore.Graphics.Render.Data;
using MintyCore.Utils;

namespace MintyCore.Graphics.Render;

/// <summary>
/// Interface for accessing input module data.
/// </summary>
[PublicAPI]
public interface IInputModuleDataAccessor
{
    /// <summary>
    /// Uses singleton input data.
    /// </summary>
    /// <typeparam name="TInputData">The type of the input data.</typeparam>
    /// <param name="inputDataId">The identification of the input data.</param>
    /// <param name="inputModule">The input module using the data.</param>
    /// <returns>The singleton input data.</returns>
    SingletonInputData<TInputData> UseSingletonInputData<TInputData>(Identification inputDataId,
        InputModule inputModule) where TInputData : notnull;

    /// <summary>
    /// Uses dictionary input data.
    /// </summary>
    /// <typeparam name="TKey">The type of the key.</typeparam>
    /// <typeparam name="TData">The type of the data.</typeparam>
    /// <param name="inputDataId">The identification of the input data.</param>
    /// <param name="inputModule">The input module using the data.</param>
    /// <returns>The dictionary input data.</returns>
    DictionaryInputData<TKey, TData> UseDictionaryInputData<TKey, TData>(Identification inputDataId,
        InputModule inputModule) where TKey : notnull;

    /// <summary>
    /// Uses intermediate data.
    /// </summary>
    /// <typeparam name="TIntermediateData">The type of the intermediate data.</typeparam>
    /// <param name="intermediateDataId">The identification of the intermediate data.</param>
    /// <param name="inputModule">The input module using the data.</param>
    /// <returns>A function that returns the current intermediate data.</returns>

    Func<TIntermediateData> UseIntermediateData<TIntermediateData>(Identification intermediateDataId,
        InputModule inputModule) where TIntermediateData : IntermediateData;

    /// <summary>
    /// Provides intermediate data.
    /// </summary>
    /// <typeparam name="TIntermediateData">The type of the intermediate data.</typeparam>
    /// <param name="intermediateDataId">The identification of the intermediate data.</param>
    /// <param name="inputModule">The input module providing the data.</param>
    /// <returns>A function that returns the current intermediate data.</returns>
    Func<TIntermediateData> ProvideIntermediateData<TIntermediateData>(Identification intermediateDataId,
        InputModule inputModule) where TIntermediateData : IntermediateData;
}