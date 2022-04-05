using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using MintyCore.Utils;

namespace MintyCore.ECS;

/// <summary>
///     Holds the complete entity data for a specific archetype
///     <remarks>Not intended for public use, as no safety checks are done at this level. Only public as some features like the ComponentQuerySourceGenerator requires it</remarks>
/// </summary>
[DebuggerTypeProxy(typeof(DebugView))]
public unsafe class ArchetypeStorage : IDisposable
{
    /// <summary>
    ///     Initial size of the storage (entity count)
    /// </summary>
    private const int DefaultStorageSize = 16;

    private readonly ArchetypeContainer _archetype;

    private readonly Dictionary<Identification, IntPtr> _componentArrays = new();


    /// <summary>
    /// Indices of entities in the storage
    /// </summary>
    public readonly Dictionary<Entity, int> EntityIndex = new(DefaultStorageSize);
    private int _entityCount;

    private int _storageSize = DefaultStorageSize;

    /// <summary>
    /// Entity at the given index in the storage
    /// </summary>
    public Entity[] IndexEntity = new Entity[DefaultStorageSize];


    internal ArchetypeStorage(ArchetypeContainer archetype, Identification archetypeId)
    {
        _archetype = archetype;

        foreach (var componentId in archetype.ArchetypeComponents)
        {
            var componentSize = ComponentManager.GetComponentSize(componentId);
            _componentArrays[componentId] = AllocationHandler.Malloc(componentSize * _storageSize);
        }

        Array.Resize(ref IndexEntity, _storageSize);
        Id = archetypeId;
    }

    /// <summary>
    ///     Id of the archetype stored in this storage
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public Identification Id { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        foreach (var entity in EntityIndex) RemoveEntity(entity.Key);
        foreach (var componentArray in _componentArrays.Values)
        {
            AllocationHandler.Free(componentArray);
        }

        _componentArrays.Clear();
    }

    /// <summary>
    /// Get the reference to the <see cref="TComponent"/> of the given <see cref="Entity"/>
    /// </summary>
    /// <param name="entity">Entity to get component from</param>
    /// <typeparam name="TComponent">Type of the component.</typeparam>
    /// <returns>Reference to the component</returns>
    public ref TComponent GetComponent<TComponent>(Entity entity)
        where TComponent : unmanaged, IComponent
    {
        return ref GetComponent<TComponent>(entity, default(TComponent).Identification);
    }

    /// <summary>
    /// Get the reference to the <see cref="TComponent"/> of the given <see cref="Entity"/>
    /// </summary>
    /// <param name="entity">Entity to get component from</param>
    /// <param name="componentId"><see cref="Identification"/> of the component</param>
    /// <typeparam name="TComponent">Type of the component.</typeparam>
    /// <returns>Reference to the component</returns>
    public ref TComponent GetComponent<TComponent>(Entity entity, Identification componentId)
        where TComponent : unmanaged, IComponent
    {
        return ref GetComponent<TComponent>(EntityIndex[entity], componentId);
    }

    /// <summary>
    /// Get the reference to the <see cref="TComponent"/> of the given entity index inside the storage
    /// </summary>
    /// <param name="entityIndex">Entity index to get component from</param>
    /// <param name="componentId"><see cref="Identification"/> of the component</param>
    /// <typeparam name="TComponent">Type of the component.</typeparam>
    /// <returns>Reference to the component</returns>
    public ref TComponent GetComponent<TComponent>(int entityIndex, Identification componentId)
        where TComponent : unmanaged, IComponent
    {
        //Get the base IntPtr of the component type, cast it to the ComponentPointer
        //Get the right value by the entity index and return a reference to it
        return ref ((TComponent*) _componentArrays[componentId])[entityIndex];
    }
    
    /// <summary>
    /// Get the pointer to the component by component id of the given <see cref="Entity"/>
    /// </summary>
    /// <param name="entity">Entity to get component from</param>
    /// <param name="componentId"><see cref="Identification"/> of the component</param>
    /// <returns>Pointer to the component</returns>
    public IntPtr GetComponentPtr(Entity entity, Identification componentId)
    {
        var componentSize = ComponentManager.GetComponentSize(componentId);
        var entityIndex = EntityIndex[entity];
        return _componentArrays[componentId] + componentSize * entityIndex;
    }
    
    /// <summary>
    /// Get the pointer to the component by component id of the given entity index inside the storage
    /// </summary>
    /// <param name="entityIndex">Entity index to get component from</param>
    /// <param name="componentId"><see cref="Identification"/> of the component</param>
    /// <returns>Pointer to the component</returns>
    public IntPtr GetComponentPtr(int entityIndex, Identification componentId)
    {
        var componentSize = ComponentManager.GetComponentSize(componentId);
        return _componentArrays[componentId] + componentSize * entityIndex;
    }

    internal bool AddEntity(Entity entity)
    {
        if (EntityIndex.ContainsKey(entity)) return false;

        //Check if the storage is large enough
        if (_entityCount >= _storageSize) Resize(_entityCount * 2);

        var entityIndex = _entityCount;


        EntityIndex.Add(entity, entityIndex);
        IndexEntity[entityIndex] = entity;
        _entityCount++;

        foreach (var componentId in _archetype.ArchetypeComponents)
        {
            //Populate all components of the newly created entity with their default values
            ComponentManager.PopulateComponentDefaultValues(componentId,
                _componentArrays[componentId] + ComponentManager.GetComponentSize(componentId) * entityIndex);
        }

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


        foreach (var componentId in _archetype.ArchetypeComponents)
        {
            var componentSize = ComponentManager.GetComponentSize(componentId);
            var componentPtr = _componentArrays[componentId] + componentSize * index;
            ComponentManager.CastPtrToIComponent(componentId, componentPtr).DecreaseRefCount();
        }

        EntityIndex.Remove(entity);
        IndexEntity[index] = default;
        _entityCount--;

        //By comparing the current entity count with the index of the deleted entity we can check if we destroyed the last entity in the storage
        //If it was not the last one, move the last entity to the index of the deleted one to prevent a hole
        if (_entityCount != index)
        {
            var entityIndexToMove = _entityCount;
            var entityToMove = IndexEntity[entityIndexToMove];

            //Move the entity struct to its new locations
            IndexEntity[entityIndexToMove] = default;
            IndexEntity[index] = entityToMove;
            EntityIndex[entityToMove] = index;

            //Move the components to their new location
            foreach (var componentId in _archetype.ArchetypeComponents)
            {
                var componentSize = ComponentManager.GetComponentSize(componentId);
                var sourceComponentPtr = _componentArrays[componentId] + componentSize * entityIndexToMove;
                var targetComponentPtr = _componentArrays[componentId] + componentSize * index;

                var sourceComponentData = new Span<byte>((void*) sourceComponentPtr, componentSize);
                var targetComponentData = new Span<byte>((void*) targetComponentPtr, componentSize);

                sourceComponentData.CopyTo(targetComponentData);
            }
        }

        //Check if the storage is small enough to decrease the size
        if (_entityCount * 4 <= _storageSize && _storageSize > DefaultStorageSize) Resize(_storageSize / 2);
    }

    private void Resize(int newSize)
    {
        if (newSize == _storageSize) return;

        if (newSize < _storageSize)
        {
            Logger.AssertAndThrow(newSize >= _entityCount,
                $"The new size ({newSize}) of the archetype storage is smaller then the current entity count ({_entityCount})",
                "ECS");
        }

        //Create new component data arrays and move and dispose the old data
        foreach (var componentId in _archetype.ArchetypeComponents)
        {
            var componentSize = ComponentManager.GetComponentSize(componentId);
            var oldData = _componentArrays[componentId];
            var newData = AllocationHandler.Malloc(componentSize * newSize);

            //Copy the old data to the new location, using spans as they are highly optimized
            var oldDataSpan = new Span<byte>((void*) oldData, componentSize * _entityCount);
            var newDataSpan = new Span<byte>((void*) newData, componentSize * newSize);
            oldDataSpan.CopyTo(newDataSpan);

            AllocationHandler.Free(oldData);
            _componentArrays[componentId] = newData;
        }

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
        private ArchetypeStorage _parent;

        //The enumerator and the entity index starts both with an invalid value
        private readonly Identification[] _archetypeComponents;
        private readonly int[] _componentSizes;
        private readonly ulong _componentCount;
        private readonly byte*[] _componentDatas;
        private readonly ulong[] _dirtyOffsets;

        private readonly Entity[] _entityIndexes;
        private byte* _currentCmpPtr;
        private ulong _currentComponentDirtyOffset;

        private ulong _currentComponentIndex;
        private byte* _currentDirtyPtr;
        private ulong _currentEntityIndex;

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="parent"><see cref="ArchetypeStorage" /> to enumerate over</param>
        public DirtyComponentQuery(ArchetypeStorage parent)
        {
            _parent = parent;

            //collect all needed data, to dont need to store a reference to the parent
            _archetypeComponents = new Identification[parent._archetype.ArchetypeComponents.Count];
            _dirtyOffsets = new ulong[_archetypeComponents.Length];
            _componentCount = (ulong) _archetypeComponents.Length;
            _componentDatas = new byte*[_archetypeComponents.Length];
            _componentSizes = new int[_archetypeComponents.Length];
            _entityIndexes = parent.IndexEntity;

            var i = 0;
            foreach (var component in parent._archetype.ArchetypeComponents)
            {
                _archetypeComponents[i] = component;
                _dirtyOffsets[i] = (ulong) ComponentManager.GetDirtyOffset(component);
                _componentDatas[i] = (byte*) parent.GetComponentPtr(0, component);
                _componentSizes[i] = ComponentManager.GetComponentSize(component);
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
            if (!EntityIndexValid())
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
            _currentComponentDirtyOffset = _dirtyOffsets[_currentComponentIndex];
            _currentCmpPtr = _componentDatas[_currentComponentIndex];
            _currentDirtyPtr = _currentCmpPtr + _currentComponentDirtyOffset;

            _currentEntityIndex = 0;
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

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        private bool EntityIndexValid()
        {
            return _currentEntityIndex < (ulong) _parent._entityCount;
        }

        private bool FindNextEntity()
        {
            _currentEntityIndex++;
            if (!EntityIndexValid()) return false;

            _currentCmpPtr += _componentSizes[_currentComponentIndex];
            _currentDirtyPtr += _componentSizes[_currentComponentIndex];

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