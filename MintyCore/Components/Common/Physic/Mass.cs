using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.Utils;

namespace MintyCore.Components.Common.Physic
{
	public struct Mass : IComponent
	{
		public byte Dirty { get; set; }

		private float _inverseMass;

		public float MassValue
		{
			get => 1 / _inverseMass;
			set => _inverseMass = 1 / value;
		}

		public float InverseMass
		{
			get => _inverseMass;
			set => _inverseMass = value;
		}

		public void SetInfiniteMass()
		{
			_inverseMass = 0;
		}

		public Identification Identification => ComponentIDs.Mass;

		public void Deserialize(DataReader reader)
		{
			_inverseMass = reader.GetFloat();
		}

		public void PopulateWithDefaultValues()
		{
			_inverseMass = 0;
		}

		public void Serialize(DataWriter writer)
		{
			writer.Put(_inverseMass);
		}

		public void IncreaseRefCount()
		{
		}

		public void DecreaseRefCount()
		{
		}
	}
}
