using System;

namespace MintyCore.Utils;

/// <summary>
///     General Exception type for all
/// </summary>
public class MintyCoreException : Exception
{
    internal MintyCoreException()
    {
    }

    internal MintyCoreException(string? message) : base(message)
    {
    }

    internal MintyCoreException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}