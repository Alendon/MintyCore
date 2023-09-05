using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;
using MintyCore.Utils.Maths;

namespace MintyCore.Utils;

public static class AllocationTracker
{
    private static readonly Dictionary<object, (StackTrace?, ModState)> _allocations = new();

    [PublicAPI]
    public static void TrackAllocation(object obj)
    {
        StackTrace? stackTrace = null;
        if (Engine.TestingModeActive)
        {
            stackTrace = new StackTrace(1, true);
        }

        lock (_allocations)
            _allocations.Add(obj, (stackTrace, Engine.ModState));
    }

    [PublicAPI]
    public static void RemoveAllocation(object obj)
    {
        lock (_allocations)
        {
            _allocations.Remove(obj);
        }
    }

    public static void CheckForLeaks(ModState stateToCheck)
    {
        lock (_allocations)
        {
            foreach (var (obj, (stackTrace, state)) in _allocations)
            {
                if (MathHelper.IsBitSet((int) state, (int) stateToCheck)) continue;
                
                Logger.WriteLog($"Found leaked object of type {obj.GetType().Name}!", LogImportance.Error,
                    "AllocationTracker");
                
                if (stackTrace is not null)
                {
                    Logger.WriteLog($"Allocation stacktrace: {stackTrace}", LogImportance.Error, "AllocationTracker");
                }
            }
        }
    }
}