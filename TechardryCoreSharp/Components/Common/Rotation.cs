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
	struct Rotation : IComponent
	{
		Vector3 Value;

		public byte Dirty { get; set; }

		public Identification Identification => ComponentIDs.Rotation;

		public void Deserialize( DataReader reader )
		{
			Value = reader.GetVector3();
		}
		public void PopulateWithDefaultValues()
		{
			Value = Vector3.Zero;
		}
		public void Serialize( DataWriter writer )
		{
			writer.Put( Value );
		}
	}
}
