using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MintyCore.Utils;

namespace MintyCore.ECS
{
	/// <summary>
	/// Interface for each entity component struct
	/// </summary>
	public interface IComponent : IDisposable
	{
		/// <summary>
		/// Specify if a component is dirty (aka changed in the last tick) or not
		/// </summary>
		byte Dirty { get; set; }

		/// <summary>
		/// Set the default values for the component
		/// </summary>
		void PopulateWithDefaultValues();

		/// <summary>
		/// Get the identification for the component
		/// </summary>
		Identification Identification { get; }

		/// <summary>
		/// Serialize the data of the component
		/// </summary>
		/// <param name="writer">The DataWriter to serialize with</param>
		void Serialize( DataWriter writer );

		/// <summary>
		/// Deserialize the data of the component
		/// </summary>
		/// <param name="reader"></param>
		void Deserialize( DataReader reader );

	}
}
