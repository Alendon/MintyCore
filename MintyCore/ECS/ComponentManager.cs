using System;
using System.Collections.Generic;
using System.Linq;
using MintyCore.Utils;

namespace MintyCore.ECS
{
	/// <summary>
	///     Class to manage component stuff at init and runtime
	/// </summary>
	public static class ComponentManager
    {
        private static readonly Dictionary<Identification, int> _componentSizes = new();
        private static readonly Dictionary<Identification, Action<IntPtr>> _componentDefaultValues = new();
        private static readonly Dictionary<Identification, int> _componentDirtyOffset = new();
        private static readonly Dictionary<Identification, Action<IntPtr, DataWriter>> _componentSerialize = new();
        private static readonly Dictionary<Identification, Action<IntPtr, DataReader>> _componentDeserialize = new();
        private static readonly Dictionary<Identification, Func<IntPtr, IComponent>> _ptrToComponentCasts = new();
        private static readonly HashSet<Identification> _playerControlledComponents = new();

        internal static unsafe void AddComponent<T>(Identification componentId) where T : unmanaged, IComponent
        {
            if (_componentSizes.ContainsKey(componentId))
                throw new ArgumentException($"Component {componentId} is already present");

            _componentSizes.Add(componentId, sizeof(T));
            _componentDefaultValues.Add(componentId, ptr =>
            {
                *(T*)ptr = default;
                ((T*)ptr)->PopulateWithDefaultValues();
            });

            var dirtyOffset = GetDirtyOffset<T>();
            _componentDirtyOffset.Add(componentId, dirtyOffset);

            _componentSerialize.Add(componentId, (ptr, serializer) => { ((T*)ptr)->Serialize(serializer); });

            _componentDeserialize.Add(componentId, (ptr, deserializer) => { ((T*)ptr)->Deserialize(deserializer); });

            _ptrToComponentCasts.Add(componentId, ptr => *(T*)ptr);

            var componentType = typeof(T);
            if (componentType.GetCustomAttributes(false).Contains(typeof(PlayerControlledAttribute)))
                _playerControlledComponents.Add(componentId);
        }

        private static unsafe int GetDirtyOffset<T>() where T : unmanaged, IComponent
        {
            var dirtyOffset = -1;
            T first = default;
            T second = default;

            second.Dirty = 1;
            var firstPtr = (byte*)&first;
            var secondPtr = (byte*)&second;

            for (var i = 0; i < sizeof(T); i++)
                if (firstPtr[i] != secondPtr[i])
                {
                    dirtyOffset = i;
                    break;
                }

            T ptrTest1 = default;
            T ptrTest2 = default;
            ((byte*)&ptrTest1)[dirtyOffset] = 1;
            ptrTest2.Dirty = 1;

            if (dirtyOffset < 0 || second.Dirty != 1 || first.Dirty != 0 || ptrTest1.Dirty != 1 ||
                ((byte*)&ptrTest2)[dirtyOffset] != 1)
                throw new Exception("Given Component has an invalid dirty property");

            return dirtyOffset;
        }

        /// <summary>
        ///     Get the dirty offset of a <see cref="IComponent" /> in bytes. <seealso cref="IComponent.Dirty" />
        /// </summary>
        /// <param name="componentId"><see cref="Identification" /> of the component</param>
        /// <returns>Offset in bytes</returns>
        public static int GetDirtyOffset(Identification componentId)
        {
            return _componentDirtyOffset[componentId];
        }

        public static bool IsPlayerControlled(Identification componentId)
        {
            return _playerControlledComponents.Contains(componentId);
        }

        /// <summary>
        ///     Get the size in bytes of a <see cref="IComponent" />
        /// </summary>
        /// <param name="componentId"><see cref="Identification" /> of the component</param>
        /// <returns>Offset in bytes</returns>
        public static int GetComponentSize(Identification componentId)
        {
            return _componentSizes[componentId];
        }

        /// <summary>
        ///     Serialize a component
        /// </summary>
        public static void SerializeComponent(IntPtr component, Identification componentId, DataWriter dataWriter)
        {
            _componentSerialize[componentId](component, dataWriter);
        }

        /// <summary>
        ///     Deserialize a component
        /// </summary>
        public static void DeserializeComponent(IntPtr component, Identification componentId, DataReader dataReader)
        {
            _componentDeserialize[componentId](component, dataReader);
        }

        internal static void PopulateComponentDefaultValues(Identification componentId, IntPtr componentLocation)
        {
            _componentDefaultValues[componentId](componentLocation);
        }

        /// <summary>
        ///     Cast a <see cref="IntPtr" /> to <see cref="IComponent" /> by the given component <see cref="Identification" />
        /// </summary>
        /// <param name="componentId"><see cref="Identification" /> of the component</param>
        /// <param name="componentPtr">Location of the component in memory</param>
        /// <returns><see cref="IComponent" /> parent of the component</returns>
        public static IComponent CastPtrToIComponent(Identification componentId, IntPtr componentPtr)
        {
            return _ptrToComponentCasts[componentId](componentPtr);
        }


        internal static void Clear()
        {
            _componentSizes.Clear();
            _componentDefaultValues.Clear();
            _playerControlledComponents.Clear();
        }

        internal static IEnumerable<Identification> GetComponentList()
        {
            return _componentSizes.Keys;
        }
    }
}