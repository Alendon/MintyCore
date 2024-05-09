using MintyCore.Utils;

namespace MintyCore.Modding;

/// <summary>
/// Interface for objects that have an identification
/// </summary>
public interface IWithIdentification
{
    /// <summary>
    ///  The identification of the object
    /// </summary>
    static abstract Identification Identification { get; }
}