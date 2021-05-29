using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TechardryCoreSharp.Utils;

namespace TechardryCoreSharp.ECS
{

	public static class ComponentManager
	{
		private static readonly Dictionary<Identification, int> _componentSizes = new Dictionary<Identification, int>();
		private static Dictionary<Identification, Action<IntPtr>> _componentDefaultValues = new Dictionary<Identification, Action<IntPtr>>();

		internal static unsafe void AddComponent<T>( Identification componentID ) where T : unmanaged, IComponent
		{
			if ( _componentSizes.ContainsKey( componentID ) )
			{
				throw new ArgumentException( $"Component {componentID} is already present" );
			}

			_componentSizes.Add( componentID, sizeof( T ) );
			_componentDefaultValues.Add( componentID, ptr =>
			 {
				 ( ( T* )ptr )->PopulateWithDefaultValues();
			 } );
		}

		internal static int GetComponentSize( Identification componentID )
		{
			return _componentSizes[componentID];
		}

		internal static void PopulateComponentDefaultValues( Identification componentID, IntPtr componentLocation )
		{
			_componentDefaultValues[componentID]( componentLocation );
		}

		internal static void Clear()
		{
			_componentSizes.Clear();
			_componentDefaultValues.Clear();
		}

		internal static IEnumerable<Identification> GetComponentList()
		{
			return _componentSizes.Keys;
		}
	}
}
