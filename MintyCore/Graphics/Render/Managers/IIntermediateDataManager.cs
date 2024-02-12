using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using MintyCore.Graphics.Render.Data;
using MintyCore.Graphics.Render.Data.RegistryWrapper;
using MintyCore.Utils;

namespace MintyCore.Graphics.Render.Managers;

/// <summary>
/// Interface for managing intermediate data.
/// </summary>
[PublicAPI]
public interface IIntermediateDataManager
{
    /// <summary>
    /// Registers intermediate data of a specific type.
    /// </summary>
    /// <typeparam name="TIntermediateData">The type of the intermediate data.</typeparam>
    /// <param name="intermediateDataId">The identification of the intermediate data.</param>
    void RegisterIntermediateData<TIntermediateData>(Identification intermediateDataId)
        where TIntermediateData : IntermediateData, new();


    /// <summary>
    /// Registers intermediate data with a registry wrapper.
    /// </summary>
    /// <param name="intermediateDataId">The identification of the intermediate data.</param>
    /// <param name="registryWrapper">The registry wrapper for the intermediate data.</param>
    void RegisterIntermediateData(Identification intermediateDataId, IntermediateDataRegistryWrapper registryWrapper);

    /// <summary>
    /// Gets new intermediate data.
    /// </summary>
    /// <param name="intermediateDataId">The identification of the intermediate data.</param>
    /// <returns>The new intermediate data.</returns>
    IntermediateData GetNewIntermediateData(Identification intermediateDataId);

    /// <summary>
    /// Recycles intermediate data.
    /// </summary>
    /// <param name="intermediateDataId">The identification of the intermediate data.</param>
    /// <param name="data">The intermediate data to recycle.</param>
    void RecycleIntermediateData(Identification intermediateDataId, IntermediateData data);


    /// <summary>
    /// Sets the current data.
    /// </summary>
    /// <param name="intermediateDataId">The identification of the intermediate data.</param>
    /// <param name="currentData">The current data to set.</param>
    void SetCurrentData(Identification intermediateDataId, IntermediateData currentData);

    /// <summary>
    /// Gets the ids of the registered intermediate data.
    /// </summary>
    /// <returns>The ids of the registered intermediate data.</returns>
    IEnumerable<Identification> GetRegisteredIntermediateDataIds();

    /// <summary>
    /// Gets the current data.
    /// </summary>
    /// <param name="intermediateId">The identification of the intermediate data.</param>
    /// <returns>The current data.</returns>
    IntermediateData? GetCurrentData(Identification intermediateId);

    /// <summary>
    /// Unregisters intermediate data.
    /// </summary>
    /// <param name="objectId">The id of the intermediate data.</param>
    void UnRegisterIntermediateData(Identification objectId);

    /// <summary>
    /// Clear all internal data.
    /// </summary>
    void Clear();

    /// <summary>
    /// Gets the type of the intermediate data.
    /// </summary>
    /// <param name="intermediateDataId">The identification of the intermediate data.</param>
    /// <returns>The type of the intermediate data.</returns>
    Type GetIntermediateDataType(Identification intermediateDataId);
}