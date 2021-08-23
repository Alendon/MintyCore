using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Utils;

namespace MintyCore.Components.Common.Physic.Dynamics
{
	/// <summary>
	/// Component that describes the rate of changing Position (aka speed)
	/// </summary>
	public struct Velocity : IComponent
	{
		public Vector3 Value;

		public byte Dirty {  get; set; }

		public Identification Identification => ComponentIDs.Velocity;

		public void DecreaseRefCount()
		{
		}

		public void Deserialize(DataReader reader)
		{
			Value = reader.GetVector3();
		}


		public void IncreaseRefCount()
		{
		}

		public void PopulateWithDefaultValues()
		{
			Value = Vector3.Zero;
		}

		public void Serialize(DataWriter writer)
		{
			writer.Put(Value);
		}
	}
}
