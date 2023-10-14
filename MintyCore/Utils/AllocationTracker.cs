using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;
using MintyCore.Utils.Maths;

namespace MintyCore.Utils;

[Singleton<IAllocationTracker>]
public class AllocationTracker : IAllocationTracker
{
    private readonly Dictionary<object, (StackTrace?, ModState)> _allocations = new();

    [PublicAPI]
    public void TrackAllocation(object obj)
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
    public void RemoveAllocation(object obj)
    {
        lock (_allocations)
        {
            _allocations.Remove(obj);
        }
    }

    public void CheckForLeaks(ModState stateToCheck)
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