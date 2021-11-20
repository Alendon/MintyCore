using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using MintyCore.Utils;

namespace MintyCore.ECS
{
    [DebuggerTypeProxy(typeof(DebugView))]
    internal unsafe class ArchetypeStorage : IDisposable
    {
        private const int DefaultStorageSize = 16;
        private readonly ArchetypeContainer _archetype;
        public readonly int ArchetypeSize;
        public readonly Dictionary<Identification, int> ComponentOffsets = new();


        //Key: Entity, Value: Index
        internal readonly Dictionary<Entity, int> EntityIndex = new(DefaultStorageSize);
        private int _entityCount;

        private int _entityIndexSearchPivot = -1;
        private int _storageSize = DefaultStorageSize;

        //Index (of Array): Index (in Memory), Value: Entity
        internal Entity[] IndexEntity = new Entity[DefaultStorageSize];


        internal ArchetypeStorage(ArchetypeContainer archetype, Identification archetypeId)
        {
            var lastComponentOffset = 0;
            _archetype = archetype;

            foreach (var componentId in archetype.ArchetypeComponents)
            {
                var componentSize = ComponentManager.GetComponentSize(componentId);
                ArchetypeSize += componentSize;
                ComponentOffsets.Add(componentId, lastComponentOffset);
                lastComponentOffset += componentSize;
            }

            Data = AllocationHandler.Malloc(ArchetypeSize * _storageSize);
            Array.Resize(ref IndexEntity, _storageSize);
            Id = archetypeId;
        }

        public IntPtr Data { get; private set; }
        public Identification Id { get; }

        public void Dispose()
        {
            foreach (var entity in EntityIndex) RemoveEntity(entity.Key);
            AllocationHandler.Free(Data);
        }


        internal TComponent GetComponent<TComponent>(Entity entity) where TComponent : unmanaged, IComponent
        {
            TComponent component = default;
            return GetComponent<TComponent>(entity, component.Identification);
        }

        internal TComponent GetComponent<TComponent>(Entity entity, Identification componentId)
            where TComponent : unmanaged, IComponent
        {
            return *GetComponentPtr<TComponent>(entity, componentId);
        }

        internal ref TComponent GetRefComponent<TComponent>(Entity entity) where TComponent : unmanaged, IComponent
        {
            TComponent component = default;
            return ref GetRefComponent<TComponent>(entity, component.Identification);
        }

        internal ref TComponent GetRefComponent<TComponent>(Entity entity, Identification componentId)
            where TComponent : unmanaged, IComponent
        {
            return ref *GetComponentPtr<TComponent>(entity, componentId);
        }

        internal void SetComponent<TComponent>(Entity entity, TComponent component)
            where TComponent : unmanaged, IComponent
        {
            var compPtr = GetComponentPtr<TComponent>(entity, component.Identification);
            compPtr->DecreaseRefCount();
            *compPtr = component;
            compPtr->IncreaseRefCount();
        }

        internal void SetComponent<TComponent>(Entity entity, TComponent* component)
            where TComponent : unmanaged, IComponent
        {
            var compPtr = GetComponentPtr<TComponent>(entity, component->Identification);
            compPtr->DecreaseRefCount();
            *compPtr = *component;
            compPtr->IncreaseRefCount();
        }

        internal IntPtr GetComponentPtr(Entity entity, Identification componentId)
        {
            return GetComponentPtr(EntityIndex[entity], componentId);
        }

        internal IntPtr GetComponentPtr(int entityIndex, Identification componentId)
        {
            return Data + entityIndex * ArchetypeSize + ComponentOffsets[componentId];
        }

        internal TComponent* GetComponentPtr<TComponent>(Entity entity, Identification componentId)
            where TComponent : unmanaged, IComponent
        {
            return (TComponent*)GetComponentPtr(entity, componentId);
        }

        internal TComponent* GetComponentPtr<TComponent>(Entity entity) where TComponent : unmanaged, IComponent
        {
            TComponent component = default;
            return GetComponentPtr<TComponent>(entity, component.Identification);
        }

        internal TComponent* GetComponentPtr<TComponent>(int entityIndex, Identification componentId)
            where TComponent : unmanaged, IComponent
        {
            return (TComponent*)GetComponentPtr(entityIndex, componentId);
        }


        internal bool AddEntity(Entity entity)
        {
            if (EntityIndex.ContainsKey(entity)) return false;

            if (_entityCount >= _storageSize) Resize(_entityCount * 2);

            var freeIndex = _entityIndexSearchPivot;
            if (!FindNextFreeIndex(ref freeIndex))
            {
                freeIndex = -1;
                if (!FindNextFreeIndex(ref freeIndex)) throw new Exception("Unknown Error happened");
            }

            EntityIndex.Add(entity, freeIndex);
            IndexEntity[freeIndex] = entity;
            _entityCount++;
            _entityIndexSearchPivot = freeIndex;

            var entityData = Data + freeIndex * ArchetypeSize;
            foreach (var (componentId, componentOffset) in ComponentOffsets)
            {
                ComponentManager.PopulateComponentDefaultValues(componentId, entityData + componentOffset);
            }

            return true;
        }

        internal void RemoveEntity(Entity entity)
        {
            if (!EntityIndex.ContainsKey(entity)) throw new ArgumentException($" Entity {entity} not present");
            var index = EntityIndex[entity];

            foreach (var (id, offset) in ComponentOffsets)
                ComponentManager.CastPtrToIComponent(id, Data + index * ArchetypeSize + offset)
                    .DecreaseRefCount();

            EntityIndex.Remove(entity);
            IndexEntity[index] = default;
            _entityCount--;

            if (_entityCount * 4 <= _storageSize && _storageSize > DefaultStorageSize) Resize(_storageSize / 2);
        }


        /// <summary>
        ///     Find next free entityIndex
        /// </summary>
        /// <param name="previousIndex">The last known free index. If unknown use -1</param>
        /// <returns>Returns true if an free index was found</returns>
        private bool FindNextFreeIndex(ref int previousIndex)
        {
            do
            {
                previousIndex++;
                if (previousIndex >= _storageSize) return false;
            } while (IndexEntity[previousIndex] != default);

            return true;
        }

        private bool FindPreviousTakenIndex(ref int previousIndex)
        {
            do
            {
                previousIndex--;
                if (previousIndex < 0) return false;
            } while (IndexEntity[previousIndex] == default);

            return true;
        }

        private void CopyEntityFromTo(int oldEntityIndex, int newEntityIndex)
        {
            var oldDataLocation = (void*)(Data + oldEntityIndex * ArchetypeSize);
            var newDataLocation = (void*)(Data + newEntityIndex * ArchetypeSize);

            Buffer.MemoryCopy(oldDataLocation, newDataLocation, ArchetypeSize, ArchetypeSize);
        }

        private void CompactData()
        {
            var freeIndex = -1;
            var takenIndex = _storageSize;

            while (true)
            {
                if (!FindNextFreeIndex(ref freeIndex) || !FindPreviousTakenIndex(ref takenIndex)) break;
                if (freeIndex >= takenIndex) break;

                var entity = IndexEntity[takenIndex];

                EntityIndex[entity] = freeIndex;
                IndexEntity[takenIndex] = default;
                IndexEntity[freeIndex] = entity;

                CopyEntityFromTo(takenIndex, freeIndex);
            }
        }

        private void Resize(int newSize)
        {
            if (newSize == _storageSize) return;

            if (newSize < _storageSize)
            {
                if (newSize < _entityCount)
                    throw new Exception(
                        $"The new size ({newSize}) of the archetype storage is smaller then the current entity count ({_entityCount})");
                CompactData();
            }

            var oldData = (void*)Data;
            var newData = (void*)AllocationHandler.Malloc(newSize * ArchetypeSize);

            var bytesToCopy = newSize > _storageSize ? _storageSize * ArchetypeSize : newSize * ArchetypeSize;

            Buffer.MemoryCopy(oldData, newData, bytesToCopy, bytesToCopy);

            AllocationHandler.Free((IntPtr)oldData);
            Data = (IntPtr)newData;
            Array.Resize(ref IndexEntity, newSize);
            _storageSize = newSize;
        }

        public class DebugView
        {
            private ArchetypeStorage _parent;

            public DebugView(ArchetypeStorage parent)
            {
                _parent = parent;
            }

            public (Entity entity, IComponent[] components)[] EntityComponents
            {
                get
                {
                    var returnValue = new (Entity entity, IComponent[] components)[_parent._entityCount];
                    var iteration = 0;
                    foreach (var (entity, _) in _parent.EntityIndex)
                    {
                        returnValue[iteration].entity = entity;
                        returnValue[iteration].components =
                            new IComponent[_parent._archetype.ArchetypeComponents.Count];

                        var componentIteration = 0;
                        foreach (var component in _parent._archetype.ArchetypeComponents)
                        {
                            returnValue[iteration].components[componentIteration] =
                                ComponentManager.CastPtrToIComponent(component,
                                    _parent.GetComponentPtr(entity, component));
                            componentIteration++;
                        }

                        iteration++;
                    }

                    return returnValue;
                }
            }
        }

        internal DirtyComponentEnumerable GetDirtyEnumerator()
        {
            return new DirtyComponentEnumerable(this);
        }

        internal class DirtyComponentEnumerable : IEnumerable<CurrentComponent>
        {
            public ArchetypeStorage Storage;

            public DirtyComponentEnumerable(ArchetypeStorage storage)
            {
                Storage = storage;
            }

            public IEnumerator<CurrentComponent> GetEnumerator()
            {
                return new DirtyComponentQuery(Storage);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        internal class DirtyComponentQuery : IEnumerator<CurrentComponent>
        {
            //The enumerator and the entity index starts both with an invalid value
            private readonly Identification[] _archetypeComponents;
            private readonly ulong _archetypeSize;
            private readonly ulong _componentCount;
            private readonly ulong[] _componentOffsets;
            private readonly byte* _data;
            private readonly ulong[] _dirtyOffsets;
            private readonly ulong _entityCapacity;

            private readonly Entity[] _entityIndexes;
            private byte* _currentCmpPtr;
            private ulong _currentComponentDirtyOffset;

            private ulong _currentComponentIndex;
            private ulong _currentComponentOffset;
            private byte* _currentDirtyPtr;
            private ulong _currentEntityIndex;

            public DirtyComponentQuery(ArchetypeStorage parent)
            {
                _archetypeComponents = new Identification[parent._archetype.ArchetypeComponents.Count];
                _componentOffsets = new ulong[_archetypeComponents.Length];
                _dirtyOffsets = new ulong[_archetypeComponents.Length];
                _componentCount = (ulong)_archetypeComponents.Length;
                _entityIndexes = parent.IndexEntity;
                _entityCapacity = (ulong)_entityIndexes.Length;
                _archetypeSize = (ulong)parent.ArchetypeSize;
                _data = (byte*)parent.Data;

                var i = 0;
                foreach (var component in parent._archetype.ArchetypeComponents)
                {
                    _archetypeComponents[i] = component;
                    _componentOffsets[i] = (ulong)parent.ComponentOffsets[component];
                    _dirtyOffsets[i] = (ulong)ComponentManager.GetDirtyOffset(component);

                    i++;
                }

                SetNextComponentData();
                _currentEntityIndex = 0;
            }

            public CurrentComponent Current =>
                new()
                {
                    ComponentId = _archetypeComponents[_currentComponentIndex],
                    Entity = _entityIndexes[_currentEntityIndex],
                    ComponentPtr = new IntPtr(_currentCmpPtr)
                };

            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }

            /// <summary>
            ///     Move the Iterator to the next Component marked as dirty and removes the dirty flag
            ///     Dont reset the dirty flag, as this will generate an endless loop
            /// </summary>
            /// <returns>True if a component was found which is marked as dirty</returns>
            public bool MoveNext()
            {
                if (_currentEntityIndex < _entityCapacity &&
                    _entityIndexes[_currentEntityIndex].ArchetypeId.numeric == 0)
                {
                    if (!FindNextEntity())
                        return false;
                }

                while (!CurrentDirty())
                    if (!FindNextEntity() && !NextComponent())
                        return false;

                UnsetDirty();
                return true;
            }

            public void Reset()
            {
                throw new NotSupportedException();
            }

            private void SetNextComponentData()
            {
                _currentComponentOffset = _componentOffsets[_currentComponentIndex];
                _currentComponentDirtyOffset = _dirtyOffsets[_currentComponentIndex];
                _currentDirtyPtr = _data + _currentComponentOffset + _currentComponentDirtyOffset;
                _currentCmpPtr = _data + _currentComponentOffset;

                _currentEntityIndex = 0;
                if (_entityIndexes[_currentEntityIndex].ArchetypeId.numeric == 0)
                    FindNextEntity();
            }

            [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
            private bool NextComponent()
            {
                _currentComponentIndex++;
                if (!ComponentIndexValid()) return false;
                SetNextComponentData();
                return true;

            }

            [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
            private void UnsetDirty()
            {
                *_currentDirtyPtr = 0;
            }

            [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
            private bool CurrentDirty()
            {
                return *_currentDirtyPtr != 0;
            }

            [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
            private bool ComponentIndexValid()
            {
                return _currentComponentIndex < _componentCount;
            }

            private bool FindNextEntity()
            {
                var lastEntityIndex = _currentEntityIndex;
                do
                {
                    _currentEntityIndex++;
                    if (_currentEntityIndex >= _entityCapacity) return false;
                } while (_entityIndexes[_currentEntityIndex].ArchetypeId.numeric == 0);

                var offset = (_currentEntityIndex - lastEntityIndex) * _archetypeSize;
                _currentCmpPtr += offset;
                _currentDirtyPtr += offset;

                return true;
            }
        }

        public struct CurrentComponent
        {
            public Entity Entity;
            public Identification ComponentId;
            public IntPtr ComponentPtr;
        }
    }
}