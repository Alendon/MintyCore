using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechardryCoreSharp.ECS;
using TechardryCoreSharp.Utils;

namespace TechardryCoreSharp.Components.Common
{
	struct Scale : IComponent
	{
		float Value;

		public byte Dirty { get; set; }

		public Identification Identification => ComponentIDs.Scale;

		public void Deserialize( DataReader reader ) => Value = reader.GetFloat();
		public void PopulateWithDefaultValues() => Value = 1f;
		public void Serialize( DataWriter writer ) => writer.Put( Value );
	}
}
