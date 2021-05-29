using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TechardryCoreSharp.Utils
{
	public static class AllocationHandler
	{
		private static Dictionary<IntPtr, StackTrace> Allocations = new Dictionary<IntPtr, StackTrace>();

		private static void AddAllocationToTrack( IntPtr allocation )
		{
#if DEBUG
			Allocations.Add( allocation, new StackTrace( 2 ) );
#else
			Allocations.Add( allocation, null );
#endif
		}

		private static bool RemoveAllocationToTrack( IntPtr allocation )
		{

			return Allocations.Remove( allocation );

#pragma warning disable CS0162 // Warning as this is unreachable code in Debug Mode
			return true;
#pragma warning restore CS0162
		}

		internal static void CheckUnfreed()
		{

			if ( Allocations.Count == 0 )
			{
				return;
			}

			Console.WriteLine( $"{Allocations.Count} allocations were not freed." );
#if DEBUG
			Console.WriteLine( "Allocated at:" );
			foreach ( var entry in Allocations )
			{
				Console.WriteLine( entry.Value );
				Console.WriteLine( "" );
			}
#endif
		}

		public static IntPtr Malloc( int size )
		{
			IntPtr allocation = Marshal.AllocHGlobal( size );

			AddAllocationToTrack( allocation );

			return allocation;
		}

		public static unsafe IntPtr Malloc<T>(int count = 1) where T : unmanaged
		{
			IntPtr allocation = Marshal.AllocHGlobal(sizeof(T) * count);
			
			AddAllocationToTrack( allocation );

			return allocation;
		}

		public static void Free( IntPtr allocation )
		{
			if ( !RemoveAllocationToTrack( allocation ) )
			{
				throw new Exception( $"Tried to free {allocation}, but the allocation wasn't tracked internally" );
			}

			Marshal.FreeHGlobal( allocation );
		}

		public static bool AllocationValid(IntPtr allocation )
		{
			return Allocations.ContainsKey( allocation );
		}
	}
}
