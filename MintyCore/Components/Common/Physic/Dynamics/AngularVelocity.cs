﻿using System;
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
	/// Component that describes the change of rotation (aka rotation speed)
	/// </summary>
	struct AngularVelocity : IComponent
	{
		public Vector3 Value;

		public byte Dirty {  get; set; }

		public Identification Identification => ComponentIDs.AngularVelocity;

		public void Deserialize(DataReader reader)
		{
			Value = reader.GetVector3();
		}

		public void Dispose()
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