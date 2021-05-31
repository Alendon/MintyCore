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
	public struct Rotation : IComponent
	{
		public Quaternion Value;

		public byte Dirty { get; set; }

		public Identification Identification => ComponentIDs.Rotation;

		public void Deserialize( DataReader reader )
		{
			Value = reader.GetQuaternion();
		}

		public void Dispose() => throw new NotImplementedException();

		public void PopulateWithDefaultValues()
		{
			Value = Quaternion.Zero;
		}
		public void Serialize( DataWriter writer )
		{
			writer.Put( Value );
		}
	}
}
