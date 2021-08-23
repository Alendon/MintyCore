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
	/// Component that describes the "mass" of an object in the rotational context based on the offset to the origin of an entity
	/// </summary>
	public struct Inertia : IComponent
	{
		private Matrix4x4 _inverseInertiaTensor;

		public Matrix4x4 InertiaTensor
		{
			get
			{
				Matrix4x4.Invert(_inverseInertiaTensor, out var inertia);
				return inertia;
			}
			set
			{
				Matrix4x4.Invert(value, out _inverseInertiaTensor);
			}
		}

		public Matrix4x4 InverseInertiaTensor
		{
			get => _inverseInertiaTensor;
			set => _inverseInertiaTensor = value;
		}

		public byte Dirty { get; set; }

		public Identification Identification => ComponentIDs.Inertia;

		public void DecreaseRefCount()
		{
		}

		public void Deserialize(DataReader reader)
		{
			_inverseInertiaTensor = reader.GetMatrix4x4();
		}


		public void IncreaseRefCount()
		{
		}

		public void PopulateWithDefaultValues()
		{
			_inverseInertiaTensor = Matrix4x4.Identity;
		}

		public void Serialize(DataWriter writer)
		{
			writer.Put(_inverseInertiaTensor);
		}
	}
}
