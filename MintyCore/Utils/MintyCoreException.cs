using System;
using System.Runtime.Serialization;

namespace MintyCore.Utils
{
    internal class MintyCoreException : Exception
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