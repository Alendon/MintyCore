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
	/// Component to store the scale value of an entity
	/// </summary>
	public struct Scale : IComponent
	{
		/// <summary>
		/// The scale of an entity as a float
		/// </summary>
		public float Value;

		/// <inheritdoc />
		public byte Dirty { get; set; }

		/// <inheritdoc />
		public Identification Identification => ComponentIDs.Scale;

		/// <inheritdoc />
		public void Deserialize(DataReader reader) => Value = reader.GetFloat();
		/// <inheritdoc />
		public void Dispose() => throw new NotImplementedException();
		/// <inheritdoc />
		public void PopulateWithDefaultValues() => Value = 1f;
		/// <inheritdoc />
		public void Serialize(DataWriter writer) => writer.Put(Value);
	}
}
