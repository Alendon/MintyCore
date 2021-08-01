using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using MintyCore.Utils;

namespace MintyCore.ECS
{
	/// <summary>
	/// Class to manage component stuff at init and runtime
	/// </summary>
	public static class ComponentManager
	{
		private static readonly Dictionary<Identification, int> _componentSizes = new();
		private static Dictionary<Identification, Action<IntPtr>> _componentDefaultValues = new();
		private static Dictionary<Identification, int> _componentDirtyOffset = new();
		private static Dictionary<Identification, Action<IntPtr, DataWriter>> _componentSerialize = new();
		private static Dictionary<Identification, Action<IntPtr, DataReader>> _componentDeserialize = new();
		private static Dictionary<Identification, Func<IntPtr, IComponent>> _ptrToComponentCasts = new();

		internal static unsafe void AddComponent<T>(Identification componentID) where T : unmanaged, IComponent
		{
			if (_componentSizes.ContainsKey(componentID))
			{
				throw new ArgumentException($"Component {componentID} is already present");
			}

			_componentSizes.Add(componentID, sizeof(T));
			_componentDefaultValues.Add(componentID, ptr =>
			{
				*(T*)ptr = default;
				((T*)ptr)->PopulateWithDefaultValues();
			});

			int dirtyOffset = GetDirtyOffset<T>();
			_componentDirtyOffset.Add(componentID, dirtyOffset);

			_componentSerialize.Add(componentID, (ptr, serializer) =>
			{
				((T*)ptr)->Serialize(serializer);
			});

			_componentDeserialize.Add(componentID, (ptr, deserializer) =>
			{
				((T*)ptr)->Deserialize(deserializer);
			});

			_ptrToComponentCasts.Add(componentID, (ptr) =>
			{
				return *(T*)ptr;
			});
		}

		private static unsafe int GetDirtyOffset<T>() where T : unmanaged, IComponent
		{
			int dirtyOffset = -1;
			T first = default;
			T second = default;

			second.Dirty = 1;
			byte* firstPtr = (byte*)&first;
			byte* secondPtr = (byte*)&second;

			for (int i = 0; i < sizeof(T); i++)
			{
				if (firstPtr[i] != secondPtr[i])
				{
					dirtyOffset = i;
					break;
				}
			}

			T ptrTest1 = default;
			T ptrTest2 = default;
			((byte*)&ptrTest1)[dirtyOffset] = 1;
			ptrTest2.Dirty = 1;

			if (dirtyOffset < 0 || second.Dirty != 1 || first.Dirty != 0 || ptrTest1.Dirty != 1 || ((byte*)&ptrTest2)[dirtyOffset] != 1)
			{
				throw new Exception("Given Component has an invalid dirty property");
			}

			return dirtyOffset;
		}

		/// <summary>
		/// Get the dirty offset of a <see cref="IComponent"/> in bytes. <seealso cref="IComponent.Dirty"/>
		/// </summary>
		/// <param name="componentID"><see cref="Identification"/> of the component</param>
		/// <returns>Offset in bytes</returns>
		public static int GetDirtyOffset(Identification componentID)
		{
			return _componentDirtyOffset[componentID];
		}

		/// <summary>
		/// Get the size in bytes of a <see cref="IComponent"/>
		/// </summary>
		/// <param name="componentID"><see cref="Identification"/> of the component</param>
		/// <returns>Offset in bytes</returns>
		public static int GetComponentSize(Identification componentID)
		{
			return _componentSizes[componentID];
		}

		internal static void PopulateComponentDefaultValues(Identification componentID, IntPtr componentLocation)
		{
			_componentDefaultValues[componentID](componentLocation);
		}

		/// <summary>
		/// Cast a <see cref="IntPtr"/> to <see cref="IComponent"/> by the given component <see cref="Identification"/>
		/// </summary>
		/// <param name="componentID"><see cref="Identification"/> of the component</param>
		/// <param name="componentPtr">Location of the component in memory</param>
		/// <returns><see cref="IComponent"/> parent of the component</returns>
		public static IComponent CastPtrToIComponent(Identification componentID, IntPtr componentPtr)
		{
			return _ptrToComponentCasts[componentID](componentPtr);
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
