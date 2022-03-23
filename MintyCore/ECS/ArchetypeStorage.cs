using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using MintyCore.Utils;

namespace MintyCore.ECS;

/// <summary>
///     Holds the complete entity data for a specific archetype
///     <remarks>Not intended for public use. Only public for developing a potential better GameLoop</remarks>
/// </summary>
[DebuggerTypeProxy(typeof(DebugView))]
public unsafe class ArchetypeStorage : IDisposable
{
    /// <summary>
    ///     Initial size of the storage (entity count)
    /// </summary>
    private const int DefaultStorageSize = 16;

    private readonly ArchetypeContainer _archetype;

    /// <summary>
    ///     The size of the archetype in bytes
    /// </summary>
    public readonly int ArchetypeSize;

    /// <summary>
    ///     The offsets of the individual components in bytes from the base entity pointer
    /// </summary>
    internal readonly Dictionary<Identification, int> ComponentOffsets = new();


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

        //Calculate the archetype size and each component offset (for pointer arithmetics)
        foreach (var componentId in archetype.ArchetypeComponents)
        {
            var componentSize = ComponentManager.GetComponentSize(componentId);
            ArchetypeSize += componentSize;
            ComponentOffsets.Add(componentId, lastComponentOffset);
            lastComponentOffset += componentSize;
        }

        //Allocate the initial data
        Data = AllocationHandler.Malloc(ArchetypeSize * _storageSize);
        Array.Resize(ref IndexEntity, _storageSize);
        Id = archetypeId;
    }

    /// <summary>
    ///     The pointer to the actual data of the storage.
    ///     The data is stored as an array of the component data of entities
    ///     e.g. Data = Entity[] {Entity1: {ComponentX,ComponentY,ComponentZ}, Entity2: {ComponentX, ComponentY,ComponentZ},
    ///     Entity3: {...}};
    /// </summary>
    internal IntPtr Data { get; private set; }

    /// <summary>
    ///     Id of the archetype stored in this storage
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public Identification Id { get; }

    /// <inheritdoc />
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
        //De/Increase the reference count, to allow custom resource tracking. (Allocating/Freeing unmanaged memory)
        var compPtr = GetComponentPtr<TComponent>(entity, component.Identification);
        compPtr->DecreaseRefCount();
        *compPtr = component;
        compPtr->IncreaseRefCount();
    }

    internal void SetComponent<TComponent>(Entity entity, TComponent* component)
        where TComponent : unmanaged, IComponent
    {
        //De/Increase the reference count, to allow custom resource tracking. (Allocating/Freeing unmanaged memory)
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
        return (TComponent*) GetComponentPtr(entity, componentId);
    }

    internal TComponent* GetComponentPtr<TComponent>(Entity entity) where TComponent : unmanaged, IComponent
    {
        TComponent component = default;
        return GetComponentPtr<TComponent>(entity, component.Identification);
    }

    internal TComponent* GetComponentPtr<TComponent>(int entityIndex, Identification componentId)
        where TComponent : unmanaged, IComponent
    {
        return (TComponent*) GetComponentPtr(entityIndex, componentId);
    }


    internal bool AddEntity(Entity entity)
    {
        if (EntityIndex.ContainsKey(entity)) return false;

        //Check if the storage is large enough
        if (_entityCount >= _storageSize) Resize(_entityCount * 2);

        //Search the next free index
        var freeIndex = _entityIndexSearchPivot;
        if (!FindNextFreeIndex(ref freeIndex))
        {
            freeIndex = -1;
            FindNextFreeIndex(ref freeIndex);
        }

        EntityIndex.Add(entity, freeIndex);
        IndexEntity[freeIndex] = entity;
        _entityCount++;
        _entityIndexSearchPivot = freeIndex;

        //Iterate over each component data of the entity and set the default value
        var entityData = Data + freeIndex * ArchetypeSize;
        foreach (var (componentId, componentOffset) in ComponentOffsets)
            ComponentManager.PopulateComponentDefaultValues(componentId, entityData + componentOffset);

        return true;
    }

    internal void RemoveEntity(Entity entity)
    {
        if (!EntityIndex.ContainsKey(entity))
        {
            Logger.WriteLog($"Entity to delete {entity} not present", LogImportance.ERROR, "ECS");
            return;
        }

        var index = EntityIndex[entity];

        //Decrease the reference count of each component (for Allocating/Freeing unmanaged memory)
        foreach (var (id, offset) in ComponentOffsets)
            ComponentManager.CastPtrToIComponent(id, Data + index * ArchetypeSize + offset)
                .DecreaseRefCount();

        EntityIndex.Remove(entity);
        IndexEntity[index] = default;
        _entityCount--;

        //Check if the storage is small enough to decrease the size
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
        var oldDataLocation = (void*) (Data + oldEntityIndex * ArchetypeSize);
        var newDataLocation = (void*) (Data + newEntityIndex * ArchetypeSize);

        Buffer.MemoryCopy(oldDataLocation, newDataLocation, ArchetypeSize, ArchetypeSize);
    }

    private void CompactData()
    {
        var freeIndex = -1;
        var takenIndex = _storageSize;

        while (true)
        {
            //iterate simultaneously  from the front (searching free indices) and from the back (searching taken indices)
            //and move the data to lower indices to efficiently compact the data
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
            Logger.AssertAndThrow(!(newSize < _entityCount),
                $"The new size ({newSize}) of the archetype storage is smaller then the current entity count ({_entityCount})",
                "ECS");
            CompactData();
        }

        var oldData = (void*) Data;
        var newData = (void*) AllocationHandler.Malloc(newSize * ArchetypeSize);

        //calculate the needed bytes to copy
        var bytesToCopy = newSize > _storageSize ? _storageSize * ArchetypeSize : newSize * ArchetypeSize;

        Buffer.MemoryCopy(oldData, newData, bytesToCopy, bytesToCopy);

        AllocationHandler.Free((IntPtr) oldData);
        Data = (IntPtr) newData;
        Array.Resize(ref IndexEntity, newSize);
        _storageSize = newSize;
    }

    internal DirtyComponentEnumerable GetDirtyEnumerator()
    {
        return new DirtyComponentEnumerable(this);
    }


    private class DebugView
    {
        private ArchetypeStorage _parent;

        public DebugView(ArchetypeStorage parent)
        {
            _parent = parent;
        }

        /// <summary>
        ///     Create a human readable representation of the archetype storage, for the debug view
        /// </summary>
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

    /// <summary>
    ///     Enumerator to process all components in the storage which are marked dirty
    /// </summary>
    public class DirtyComponentQuery : IEnumerator<CurrentComponent>
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

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="parent"><see cref="ArchetypeStorage" /> to enumerate over</param>
        public DirtyComponentQuery(ArchetypeStorage parent)
        {
            //collect all needed data, to dont need to store a reference to the parent
            _archetypeComponents = new Identification[parent._archetype.ArchetypeComponents.Count];
            _componentOffsets = new ulong[_archetypeComponents.Length];
            _dirtyOffsets = new ulong[_archetypeComponents.Length];
            _componentCount = (ulong) _archetypeComponents.Length;
            _entityIndexes = parent.IndexEntity;
            _entityCapacity = (ulong) _entityIndexes.Length;
            _archetypeSize = (ulong) parent.ArchetypeSize;
            _data = (byte*) parent.Data;

            var i = 0;
            foreach (var component in parent._archetype.ArchetypeComponents)
            {
                _archetypeComponents[i] = component;
                _componentOffsets[i] = (ulong) parent.ComponentOffsets[component];
                _dirtyOffsets[i] = (ulong) ComponentManager.GetDirtyOffset(component);

                i++;
            }

            SetNextComponentData();
            _currentEntityIndex = 0;
        }

        /// <summary>
        ///     Get the current component.
        ///     Contains the Id, Entity and component pointer
        /// </summary>
        public CurrentComponent Current =>
            new()
            {
                ComponentId = _archetypeComponents[_currentComponentIndex],
                Entity = _entityIndexes[_currentEntityIndex],
                ComponentPtr = new IntPtr(_currentCmpPtr)
            };

        object IEnumerator.Current => Current;

        /// <inheritdoc />
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
            //Ensure that a valid entity is selected
            if (_currentEntityIndex < _entityCapacity &&
                _entityIndexes[_currentEntityIndex].ArchetypeId == Identification.Invalid)
                if (!FindNextEntity())
                    return false;

            //While the current selected component is not dirty, try to find the next one
            //The components will be processed in batches. Contrary to the archetype storage saving the entities in batches
            //This is done to reduce dictionary accesses (for component offsets) which have a significant performance impact at this very large usage
            while (!CurrentDirty())
                if (!FindNextEntity() && !NextComponent())
                    return false;

            //Remove the dirty flag of the current component
            UnsetDirty();
            return true;
        }

        /// <inheritdoc />
        public void Reset()
        {
            throw new NotSupportedException();
        }

        private void SetNextComponentData()
        {
            //Calculate/Set the needed data for processing the next component
            _currentComponentOffset = _componentOffsets[_currentComponentIndex];
            _currentComponentDirtyOffset = _dirtyOffsets[_currentComponentIndex];
            _currentDirtyPtr = _data + _currentComponentOffset + _currentComponentDirtyOffset;
            _currentCmpPtr = _data + _currentComponentOffset;

            _currentEntityIndex = 0;
            if (_entityIndexes[_currentEntityIndex].ArchetypeId == Identification.Invalid)
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
            } while (_entityIndexes[_currentEntityIndex].ArchetypeId == Identification.Invalid);

            var offset = (_currentEntityIndex - lastEntityIndex) * _archetypeSize;
            _currentCmpPtr += offset;
            _currentDirtyPtr += offset;

            return true;
        }
    }

    /// <summary>
    ///     The result of the Enumerator
    /// </summary>
    public struct CurrentComponent
    {
        /// <summary>
        ///     The entity the component belongs to
        /// </summary>
        public Entity Entity;

        /// <summary>
        ///     The id of the component, needed to work with the pointer
        /// </summary>
        public Identification ComponentId;

        /// <summary>
        ///     The pointer of the component data
        /// </summary>
        public IntPtr ComponentPtr;
    }
}