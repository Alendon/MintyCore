using Ara3D;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechardryCoreSharp.ECS;
using TechardryCoreSharp.Utils;

namespace TechardryCoreSharp.Components.Common
{
	public struct Transform : IComponent
	{
		public Matrix4x4 Value;

		public byte Dirty { get; set; }

		public Identification Identification => ComponentIDs.Transform;

		public void Deserialize( DataReader reader ) => Value = reader.GetMatrix4x4();
		public void Dispose() => throw new NotImplementedException();
		public void PopulateWithDefaultValues() => Value = new Matrix4x4();
		public void Serialize( DataWriter writer ) => writer.Put(Value);
	}
}
