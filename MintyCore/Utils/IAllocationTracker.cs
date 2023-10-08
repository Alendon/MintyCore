namespace MintyCore.Utils;

public interface IAllocationTracker
{
    void TrackAllocation(object obj);
    void RemoveAllocation(object obj);
    void CheckForLeaks(ModState stateToCheck);
}