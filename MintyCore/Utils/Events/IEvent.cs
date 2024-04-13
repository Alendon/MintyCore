namespace MintyCore.Utils.Events;

/// <summary>
/// Interface for an event.
/// </summary>
public interface IEvent
{
    /// <summary>
    /// Identification of the event.
    /// </summary>
    /// <remarks>The static field for the id is found at RootNamespace.Identifications.EventIDs</remarks>
    static abstract Identification Identification { get; }
    
    /// <summary>
    ///  Whether the event can be modified.
    /// </summary>
    static abstract bool ModificationAllowed { get; }
}