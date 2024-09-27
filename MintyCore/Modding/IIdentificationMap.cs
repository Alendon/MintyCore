using System;
using MintyCore.Utils;

namespace MintyCore.Modding;

/// <summary>
/// Interface for mapping identifications.
/// Used for mapping id values between different contexts.
/// </summary>
public interface IIdentificationMap
{
    /// <summary>
    /// Tries to get the mapped identification for the given source identification.
    /// </summary>
    /// <param name="src">The source identification.</param>
    /// <param name="mapped">The mapped identification if found; otherwise, the default value.</param>
    /// <returns>True if the mapping was successful; otherwise, false.</returns>
    bool TryGetMapped(Identification src, out Identification mapped);
}

/// <summary>
/// Empty implementation of <see cref="IIdentificationMap"/>
/// Will always return the same identification as the input
/// </summary>
public class EmptyIdentificationMap : IIdentificationMap
{
    private EmptyIdentificationMap()
    {
    }

    /// <summary>
    /// Gets the singleton instance of the <see cref="EmptyIdentificationMap"/>.
    /// </summary>
    public static IIdentificationMap Instance { get; } = new EmptyIdentificationMap();

    /// <summary>
    /// Tries to get the mapped identification for the given source identification.
    /// Always returns the same identification as the input.
    /// </summary>
    /// <param name="src">The source identification.</param>
    /// <param name="mapped">The mapped identification, which is the same as the input.</param>
    /// <returns>Always returns true.</returns>
    public bool TryGetMapped(Identification src, out Identification mapped)
    {
        mapped = src;
        return true;
    }
}