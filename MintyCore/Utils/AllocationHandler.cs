using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MintyCore.Utils.Maths;
using Serilog;

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
            Log.Warning("Tried to free a null pointer!");
            return;
        }

        if (!RemoveAllocationToTrack(allocation))
            throw new MintyCoreException($"Tried to free {allocation}, but the allocation wasn't tracked internally");
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
        var allAllocationsRemoved = true;
        
        Log.Information("Checking for leaks in {State} State", stateToCheck);
        
        lock (_managedAllocations)
        {
            foreach (var (obj, (stackTrace, state)) in _managedAllocations)
            {
                if (MathHelper.IsBitSet((int)state, (int)stateToCheck)) continue;
                
                Log.Error("Found leaked object of type {ObjectName}!", obj.GetType().Name);

                if (stackTrace is not null)
                {
                    Log.Error("Allocation stacktrace: {StackTrace}", stackTrace);
                }
                
                allAllocationsRemoved = false;
            }
        }

        lock (_unmanagedAllocations)
        {
            foreach (var (_, (stackTrace, state)) in _unmanagedAllocations)
            {
                if (MathHelper.IsBitSet((int)state, (int)stateToCheck)) continue;
                
                Log.Error("Found leaked unmanaged allocation!");

                if (stackTrace is not null)
                {
                    Log.Error("Allocation stacktrace: {StackTrace}", stackTrace);
                }
                
                allAllocationsRemoved = false;
            }
        }
        
        if (allAllocationsRemoved)
        {
            Log.Information("All tracked allocations have been removed");
        }
    }
}