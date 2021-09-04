using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Utils;

namespace MintyCore.Components.Client
{
	/// <summary>
	///     Component to track camera data
	/// </summary>
	[PlayerControlled]
	public struct Camera : IComponent
    {
	    /// <inheritdoc />
	    public byte Dirty { get; set; }

	    /// <summary>
	    ///     Stores the field of view
	    /// </summary>
	    public float Fov;

	    /// <summary>
	    ///     <see cref="Identification" /> of the <see cref="Camera" /> Component
	    /// </summary>
	    public Identification Identification => ComponentIDs.Camera;

	    /// <inheritdoc />
	    public void Deserialize(DataReader reader)
        {
            Fov = reader.GetFloat();
        }

	    /// <inheritdoc />
	    public void PopulateWithDefaultValues()
        {
            Fov = 1.5f;
        }

	    /// <inheritdoc />
	    public void Serialize(DataWriter writer)
        {
            writer.Put(Fov);
        }

	    /// <summary>
	    ///     Does nothing in this component
	    /// </summary>
	    public void IncreaseRefCount()
        {
        }

	    /// <summary>
	    ///     Does nothing in this component
	    /// </summary>
	    public void DecreaseRefCount()
        {
        }
    }
}