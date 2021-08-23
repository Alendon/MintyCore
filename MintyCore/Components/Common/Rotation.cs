using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
		/// Value of the rotation as <see cref="Quaternion"/>
		/// </summary>
		public Quaternion Value;

		/// <inheritdoc />
		public byte Dirty { get; set; }

		/// <inheritdoc />
		public Identification Identification => ComponentIDs.Rotation;

		public void DecreaseRefCount()
		{
		}

		/// <inheritdoc />
		public void Deserialize( DataReader reader )
		{
			Value = reader.GetQuaternion();
		}

		/// <inheritdoc />
		public void Dispose() { }

		public void IncreaseRefCount()
		{
		}

		/// <inheritdoc />
		public void PopulateWithDefaultValues()
		{
			Value = Quaternion.Identity;
		}

		/// <inheritdoc />
		public void Serialize( DataWriter writer )
		{
			writer.Put(Value);
		}
	}
}
