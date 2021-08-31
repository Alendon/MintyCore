using MintyCore.Utils;

namespace MintyCore.ECS
{
	/// <summary>
	///     Interface for each entity component struct
	/// </summary>
	public interface IComponent
    {
	    /// <summary>
	    ///     Specify if a component is dirty (aka changed in the last tick) or not
	    /// </summary>
	    byte Dirty { get; set; }

	    /// <summary>
	    ///     Get the identification for the component
	    /// </summary>
	    Identification Identification { get; }

	    /// <summary>
	    ///     Set the default values for the component
	    /// </summary>
	    void PopulateWithDefaultValues();

	    /// <summary>
	    ///     Serialize the data of the component
	    /// </summary>
	    /// <param name="writer">The DataWriter to serialize with</param>
	    void Serialize(DataWriter writer);

	    /// <summary>
	    ///     Deserialize the data of the component
	    /// </summary>
	    /// <param name="reader"></param>
	    void Deserialize(DataReader reader);

	    /// <summary>
	    ///     Gets called everytime a component is set to an entity, to allow usage tracking of unmanaged containers
	    /// </summary>
	    void IncreaseRefCount();

	    /// <summary>
	    ///     Gets called everytime a component is removed from an entity or when an entity gets destroyed
	    /// </summary>
	    void DecreaseRefCount();
    }
}