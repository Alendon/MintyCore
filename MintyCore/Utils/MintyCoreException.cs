using System;
using System.Runtime.Serialization;

namespace MintyCore.Utils
{
    /// <summary>
    /// General Exception type for all
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

        internal MintyCoreException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}