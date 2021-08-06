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
	/// Component to store the transform value of an entity (as "T:Ara3d.Matrix4x4")
	/// </summary>
	public struct Transform : IComponent
	{
		/// <summary>
		/// The value of an entities transform
		/// </summary>
		public Matrix4x4 Value;

		/// <inheritdoc />
		public byte Dirty { get; set; }

		/// <inheritdoc />
		public Identification Identification => ComponentIDs.Transform;

		/// <inheritdoc />
		public void Deserialize( DataReader reader ) { }

		/// <inheritdoc />
		public void Dispose() => throw new NotImplementedException();

		/// <inheritdoc />
		public void PopulateWithDefaultValues() => Value = new Matrix4x4();

		/// <inheritdoc />
		public void Serialize( DataWriter writer ) { }
	}
}
