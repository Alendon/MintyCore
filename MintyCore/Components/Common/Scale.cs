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
	/// Component to store the scale value of an entity
	/// </summary>
	public struct Scale : IComponent
	{
		/// <summary>
		/// The scale of an entity as a <see cref="Vector3"/>
		/// </summary>
		public Vector3 Value;

		/// <inheritdoc />
		public byte Dirty { get; set; }

		/// <inheritdoc />
		public Identification Identification => ComponentIDs.Scale;

		public void DecreaseRefCount()
		{
		}

		/// <inheritdoc />
		public void Deserialize(DataReader reader) => Value = reader.GetVector3();

		public void IncreaseRefCount()
		{
		}

		/// <inheritdoc />
		public void PopulateWithDefaultValues() => Value = Vector3.One;
		/// <inheritdoc />
		public void Serialize(DataWriter writer) => writer.Put(Value);
	}
}
