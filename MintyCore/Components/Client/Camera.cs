using System.Numerics;
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
	    /// Position Offset from the entity Position
	    /// </summary>
	    public Vector3 PositionOffset;

	    /// <summary>
	    /// The Forward Vector of the camera
	    /// </summary>
	    public Vector3 Forward;
	    
	    /// <summary>
	    /// The Upward Vector of the camera
	    /// </summary>
	    public Vector3 Upward;
	    
	    /// <summary>
	    ///     <see cref="Identification" /> of the <see cref="Camera" /> Component
	    /// </summary>
	    public Identification Identification => ComponentIDs.Camera;

	    /// <inheritdoc />
	    public void Deserialize(DataReader reader, World world, Entity entity)
        {
            Fov = reader.GetFloat();
        }

	    /// <inheritdoc />
	    public void PopulateWithDefaultValues()
        {
            Fov = 1.0f;
            PositionOffset = Vector3.Zero;
            Forward = new Vector3(0, 0, 1);
            Upward = new Vector3(0, 1, 0);
        }

	    /// <inheritdoc />
	    public void Serialize(DataWriter writer, World world, Entity entity)
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