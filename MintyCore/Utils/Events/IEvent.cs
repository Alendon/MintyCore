namespace MintyCore.Utils.Events;

public interface IEvent
{
    static abstract Identification Identification { get; }
    static abstract bool ModificationAllowed { get; }
}