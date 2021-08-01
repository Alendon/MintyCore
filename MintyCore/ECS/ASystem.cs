﻿using System;
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

	/// <summary>
	/// Abstract base class for all systems
	/// </summary>
	public abstract class ASystem : IDisposable
	{
		/// <summary>
		/// Reference to the world the system is executed in
		/// </summary>
		public World World { get; internal set; }

		/// <summary>
		/// Method to setup the system at world creation
		/// </summary>
		public abstract void Setup();

		/// <summary>
		/// Method to execute before <see cref="Execute"/> on the main thread
		/// </summary>
		public virtual void PreExecuteMainThread() { }

		/// <summary>
		/// Method to execute after <see cref="Execute"/> on the main thread
		/// </summary>
		public virtual void PostExecuteMainThread() { }

		/// <summary>
		/// Main system method. Executed on a job thread
		/// </summary>
		public abstract void Execute();

		/// <summary>
		/// Method to queue the system as a job. Only public to allow overrides
		/// </summary>
		/// <param name="dependency">Dependencies of the system. Eq other systems job handles</param>
		/// <returns>A <see cref="JobHandleCollection"/> to specifiy wether the system is completed or not</returns>
		public virtual JobHandleCollection QueueSystem( JobHandleCollection dependency )
		{
			SystemJob job = new() { system = this };
			return job.Schedule( dependency );
		}

		/// <inheritdoc />
		public abstract void Dispose();

		/// <summary>
		/// The <see cref="Identification"/> of this system
		/// </summary>
		public abstract Identification Identification { get; }
	}
}
