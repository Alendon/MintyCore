using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MintyCore.Utils;
using MintyCore.Utils.JobSystem;

namespace MintyCore.ECS
{
	class SystemJob : AJob
	{
		internal ASystem system;
		public override void Execute() => system.Execute();
	}

	public abstract class ASystem : IDisposable
	{
		public World World { get; internal set; }

		public abstract void Setup();

		public virtual void PreExecuteMainThread() { }
		public virtual void PostExecuteMainThread() { }

		public abstract void Execute();

		public virtual JobHandleCollection QueueSystem( JobHandleCollection dependency )
		{
			SystemJob job = new() { system = this };
			return job.Schedule( dependency );
		}

		public abstract void Dispose();

		public abstract Identification Identification { get; }
		public virtual bool ExecuteOnMainThread => false;
	}
}
