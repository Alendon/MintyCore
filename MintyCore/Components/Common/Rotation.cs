using Ara3D;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Utils;

namespace MintyCore.Components.Common
{
	/// <summary>
	/// Component to store the euler rotation of an entity
	/// </summary>
	public struct Rotation : IComponent
	{
		/// <summary>
		/// Value of the euler rotation
		/// </summary>
		public Vector3 Value;

		/// <inheritdoc />
		public byte Dirty { get; set; }

		/// <inheritdoc />
		public Identification Identification => ComponentIDs.Rotation;

		/// <inheritdoc />
		public void Deserialize( DataReader reader )
		{
			Value = reader.GetVector3();
		}

		/// <inheritdoc />
		public void Dispose() { }

		/// <inheritdoc />
		public void PopulateWithDefaultValues()
		{
			Value = Vector3.Zero;
		}

		/// <inheritdoc />
		public void Serialize( DataWriter writer )
		{
			writer.Put( Value );
		}
	}
}
