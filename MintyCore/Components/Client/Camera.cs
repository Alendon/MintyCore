using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MintyCore.Components.Client
{
	struct Camera : IComponent
	{
		public byte Dirty { get; set; }
		public float Fov;

		public Identification Identification => ComponentIDs.Camera;

		public void Deserialize(DataReader reader)
		{
			Fov = reader.GetFloat();
		}

		public void Dispose()
		{
			
		}

		public void PopulateWithDefaultValues()
		{
			Fov = 1.5f;
		}

		public void Serialize(DataWriter writer)
		{
			writer.Put(Fov);
		}
	}
}
