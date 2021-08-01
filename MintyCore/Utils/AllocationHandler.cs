using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MintyCore.Utils
{
	/// <summary>
	/// AllocationHandler to manage and track memory allocations
	/// </summary>
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

			Logger.WriteLog( $"{Allocations.Count} allocations were not freed.", LogImportance.WARNING, "Memory" ,null, true );
#if DEBUG
			Logger.WriteLog( "Allocated at:", LogImportance.WARNING, "Memory", null, true);
			foreach ( var entry in Allocations )
			{
				Logger.WriteLog(entry.Value.ToString(), LogImportance.WARNING, "Memory", null, true);
			}
#endif
		}

		/// <summary>
		/// Malloc memory block with the given size
		/// </summary>
		/// <param name="size"></param>
		/// <returns></returns>
		public static IntPtr Malloc( int size )
		{
			IntPtr allocation = Marshal.AllocHGlobal( size );

			AddAllocationToTrack( allocation );

			return allocation;
		}

		/// <summary>
		/// Malloc memory block with the given size
		/// </summary>
		/// <param name="size"></param>
		/// <returns></returns>
		public static IntPtr Malloc( IntPtr size )
		{
			IntPtr allocation = Marshal.AllocHGlobal( size );

			AddAllocationToTrack( allocation );

			return allocation;
		}

		/// <summary>
		/// Malloc a memory block for <paramref name="count"/> <typeparamref name="T"/>
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="count"></param>
		/// <returns></returns>
		public static unsafe IntPtr Malloc<T>(int count = 1) where T : unmanaged
		{
			IntPtr allocation = Marshal.AllocHGlobal(sizeof(T) * count);
			
			AddAllocationToTrack( allocation );

			return allocation;
		}

		/// <summary>
		/// Free an allocation
		/// </summary>
		/// <param name="allocation"></param>
		public static void Free( IntPtr allocation )
		{
			if ( !RemoveAllocationToTrack( allocation ) )
			{
				throw new Exception( $"Tried to free {allocation}, but the allocation wasn't tracked internally" );
			}

			Marshal.FreeHGlobal( allocation );
		}

		/// <summary>
		/// Check if an allocation is still valid (not freed)
		/// </summary>
		/// <param name="allocation"></param>
		/// <returns></returns>
		public static bool AllocationValid(IntPtr allocation )
		{
			return Allocations.ContainsKey( allocation );
		}
	}
}
