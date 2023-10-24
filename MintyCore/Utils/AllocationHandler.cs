using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MintyCore.Utils.Maths;

namespace MintyCore.Utils;

/// <summary>
///     AllocationHandler to manage and track memory allocations
///     This implementation is thread-safe
/// </summary>
[Singleton<IAllocationHandler>]
internal class AllocationHandler : IAllocationHandler
{
    private readonly Dictionary<IntPtr, (StackTrace?, ModState)> _unmanagedAllocations = new();
    private readonly Dictionary<object, (StackTrace?, ModState)> _managedAllocations = new();

    private void AddAllocationToTrack(IntPtr allocation)
    {
        var stackTrace = Engine.TestingModeActive ? new StackTrace(2) : null;
        lock (_unmanagedAllocations)
            _unmanagedAllocations.Add(allocation, (stackTrace, Engine.ModState));
    }

    private bool RemoveAllocationToTrack(IntPtr allocation)
    {
        lock (_unmanagedAllocations)
        {
            return _unmanagedAllocations.Remove(allocation);
        }
    }

    /// <inheritdoc/>
    public IntPtr Malloc(int size)
    {
        var allocation = Marshal.AllocHGlobal(size);

        AddAllocationToTrack(allocation);

        return allocation;
    }

    /// <inheritdoc/>
    public IntPtr Malloc(IntPtr size)
    {
        var allocation = Marshal.AllocHGlobal(size);

        AddAllocationToTrack(allocation);

        return allocation;
    }

    /// <inheritdoc/>
    public unsafe IntPtr Malloc<T>(int count = 1) where T : unmanaged
    {
        var allocation = Marshal.AllocHGlobal(sizeof(T) * count);

        AddAllocationToTrack(allocation);

        return allocation;
    }

    /// <inheritdoc />
    public unsafe Span<T> MallocSpan<T>(int count = 1) where T : unmanaged
    {
        var allocation = Marshal.AllocHGlobal(sizeof(T) * count);

        AddAllocationToTrack(allocation);
        return new Span<T>(allocation.ToPointer(), count);
    }

    /// <inheritdoc />
    public unsafe void Free<T>(Span<T> span) where T : unmanaged
    {
        Free((IntPtr)Unsafe.AsPointer(ref span[0]));
    }

    /// <inheritdoc/>
    public void Free(IntPtr allocation)
    {
        if (allocation == IntPtr.Zero)
        {
            Logger.WriteLog("Tried to free a null pointer!", LogImportance.Warning, "AllocationHandler");
            return;
        }
        
        Logger.AssertAndThrow(RemoveAllocationToTrack(allocation),
            $"Tried to free {allocation}, but the allocation wasn't tracked internally", "AllocationHandler");

        Marshal.FreeHGlobal(allocation);
    }

    /// <inheritdoc/>
    public bool AllocationValid(IntPtr allocation)
    {
        lock (_unmanagedAllocations)
        {
            return _unmanagedAllocations.ContainsKey(allocation);
        }
    }

    /// <inheritdoc/>
    public void TrackAllocation(object obj)
    {
        StackTrace? stackTrace = null;
        if (Engine.TestingModeActive)
        {
            stackTrace = new StackTrace(1, true);
        }

        lock (_managedAllocations)
            _managedAllocations.Add(obj, (stackTrace, Engine.ModState));
    }

    /// <inheritdoc/>
    public void RemoveAllocation(object obj)
    {
        lock (_managedAllocations)
        {
            _managedAllocations.Remove(obj);
        }
    }

    /// <inheritdoc/>
    public void CheckForLeaks(ModState stateToCheck)
    {
        lock (_managedAllocations)
        {
            foreach (var (obj, (stackTrace, state)) in _managedAllocations)
            {
                if (MathHelper.IsBitSet((int)state, (int)stateToCheck)) continue;

                Logger.WriteLog($"Found leaked object of type {obj.GetType().Name}!", LogImportance.Error,
                    "AllocationTracker");

                if (stackTrace is not null)
                {
                    Logger.WriteLog($"Allocation stacktrace: {stackTrace}", LogImportance.Error, "AllocationTracker");
                }
            }
        }

        lock (_unmanagedAllocations)
        {
            foreach (var (_, (stackTrace, state)) in _unmanagedAllocations)
            {
                if (MathHelper.IsBitSet((int)state, (int)stateToCheck)) continue;

                Logger.WriteLog("Found leaked unmanaged allocation!", LogImportance.Error,
                    "AllocationTracker");

                if (stackTrace is not null)
                {
                    Logger.WriteLog($"Allocation stacktrace: {stackTrace}", LogImportance.Error, "AllocationTracker");
                }
            }
        }
    }
}