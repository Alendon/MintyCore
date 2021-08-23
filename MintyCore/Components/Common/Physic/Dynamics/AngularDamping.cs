using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Utils;

namespace MintyCore.Components.Common.Physic.Dynamics
{
	public struct AngularDamping : IComponent
	{
		public float Value;

		public byte Dirty { get; set; }

		public Identification Identification => ComponentIDs.AngularDamping;

		public void DecreaseRefCount()
		{
		}

		public void Deserialize(DataReader reader)
		{
			Value = reader.GetFloat();
		}

		public void IncreaseRefCount()
		{
		}

		public void PopulateWithDefaultValues()
		{
			Value = 0.5f;
		}

		public void Serialize(DataWriter writer)
		{
			writer.Put(Value);
		}
	}
}
