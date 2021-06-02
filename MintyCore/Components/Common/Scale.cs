using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MintyCore.ECS;
using MintyCore.Utils;

namespace MintyCore.Components.Common
{
	public struct Scale : IComponent
	{
		public float Value;

		public byte Dirty { get; set; }

		public Identification Identification => ComponentIDs.Scale;

		public void Deserialize( DataReader reader ) => Value = reader.GetFloat();
		public void Dispose() => throw new NotImplementedException();
		public void PopulateWithDefaultValues() => Value = 1f;
		public void Serialize( DataWriter writer ) => writer.Put( Value );
	}
}
