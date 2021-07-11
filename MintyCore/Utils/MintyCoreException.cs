using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace MintyCore.Utils
{
	class MintyCoreException : Exception
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
