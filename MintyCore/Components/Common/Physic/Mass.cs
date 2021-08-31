using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Utils;

namespace MintyCore.Components.Common.Physic
{
	/// <summary>
	///     Store the mass of an entity
	/// </summary>
	public struct Mass : IComponent
    {
	    /// <inheritdoc />
	    public byte Dirty { get; set; }

	    /// <summary>
	    ///     Get/Set the mass
	    /// </summary>
	    public float MassValue { get; set; }

	    /// <summary>
	    ///     Set the mass value to infinity
	    /// </summary>
	    public void SetInfiniteMass()
        {
            MassValue = 0;
        }

	    /// <summary>
	    ///     <see cref="Identification" /> of the <see cref="Mass" /> Component
	    /// </summary>
	    public Identification Identification => ComponentIDs.Mass;

	    /// <inheritdoc />
	    public void Deserialize(DataReader reader)
        {
            MassValue = reader.GetFloat();
        }

	    /// <inheritdoc />
	    public void PopulateWithDefaultValues()
        {
            MassValue = 0;
        }

	    /// <inheritdoc />
	    public void Serialize(DataWriter writer)
        {
            writer.Put(MassValue);
        }

	    /// <summary>
	    ///     Does nothing
	    /// </summary>
	    public void IncreaseRefCount()
        {
        }

	    /// <summary>
	    ///     Does nothing
	    /// </summary>
	    public void DecreaseRefCount()
        {
        }
    }
}