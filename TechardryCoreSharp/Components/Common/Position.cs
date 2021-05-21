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
	struct Position : IComponent
	{
		public byte Dirty { get; set; }

		public Vector3 Value;

		public Identification Identification => ComponentIDs.Position;

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
