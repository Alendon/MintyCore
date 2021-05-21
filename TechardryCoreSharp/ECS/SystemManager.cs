using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechardryCoreSharp.Utils;

namespace TechardryCoreSharp.ECS
{
	public class ExecuteAfterAttribute : Attribute
	{
		public Type[] ExecuteAfter { get; private set; }

		public ExecuteAfterAttribute(params Type[] executeAfter)
		{
			var abstractType = typeof( ASystem );
			foreach(var type in executeAfter )
			{
				if(!type.IsAssignableFrom( abstractType ) )
				{
					throw new ArgumentException( "Types used with the ExecuteAfterAttribute have to be Assignable from ASystem" );
				}
			}
		}
	}

	public class ExecuteBeforeAttribute : Attribute
	{
		public Type[] ExecuteBefore { get; private set; }

		public ExecuteBeforeAttribute( params Type[] executeAfter )
		{
			var abstractType = typeof( ASystem );
			foreach ( var type in executeAfter )
			{
				if ( !type.IsAssignableFrom( abstractType ) )
				{
					throw new ArgumentException( "Types used with the ExecuteBeforeAttribute have to be Assignable from ASystem" );
				}
			}
		}
	}

	public class SystemManager
	{
		internal static void SetReadComponents( Identification identification, HashSet<Identification> readOnlyComponents ) => throw new NotImplementedException();
		internal static void SetWriteComponents( Identification identification, IEnumerable<Identification> enumerable ) => throw new NotImplementedException();
	}
}
