using JetBrains.Annotations;
using MintyCore.Graphics.Render.Managers;

namespace MintyCore.Graphics.Render.Data.RegistryWrapper;

/// <summary>
/// Abstract class for intermediate data registry wrapper.
/// </summary>
public abstract class IntermediateDataRegistryWrapper
{
    /// <summary>
    /// Creates a new instance of intermediate data.
    /// </summary>
    /// <param name="intermediateDataManager">The intermediate data manager.</param>
    /// <returns>A new instance of intermediate data.</returns>
    public abstract IntermediateData CreateIntermediateData(IIntermediateDataManager intermediateDataManager);
}

/// <summary>
/// Class for intermediate data registry wrapper with specific intermediate data type.
/// </summary>
/// <typeparam name="TIntermediateData">The type of the intermediate data.</typeparam>
[PublicAPI]
public class IntermediateDataRegistryWrapper<TIntermediateData> : IntermediateDataRegistryWrapper
    where TIntermediateData : IntermediateData, new()
{
    /// <summary>
    /// Creates a new instance of intermediate data with specific intermediate data type.
    /// </summary>
    /// <param name="intermediateDataManager">The intermediate data manager.</param>
    /// <returns>A new instance of intermediate data with specific intermediate data type.</returns>
    public override IntermediateData CreateIntermediateData(IIntermediateDataManager intermediateDataManager)
    {
        return new TIntermediateData { IntermediateDataManager = intermediateDataManager };
    }
}