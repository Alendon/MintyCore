using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechardryCoreSharp.Utils;

namespace TechardryCoreSharp.ECS
{
	public abstract class ASystem : IDisposable
	{
		public void Setup( World world ) { }

		public void PreExecuteMainThread( World world ) { }
		public void PostExecuteMainThread( World world ) { }

		public abstract void Execute( World world );

		public abstract void Dispose();

		public abstract Identification Identification { get; }
	}
}
