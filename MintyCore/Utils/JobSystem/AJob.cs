using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MintyCore.Utils.JobSystem
{
	/// <summary>
	/// <see langword="abstract"/> base class for each job
	/// </summary>
	public abstract class AJob
	{
		/// <summary>
		/// Dont use this field unless you know what you do
		/// </summary>
		public bool CompletedAfterExecute = true;

		/// <summary>
		/// Execute the job. 
		/// </summary>
		public abstract void Execute();

		/// <summary>
		/// Do Job Preparation
		/// </summary>
		/// <returns><see langword="true"/> if the job should be removed from the queue. If <see langword="false"/> the job will remain on the queue and gets executed multiple times</returns>
		public virtual bool PrepareJob()
		{
			return true;
		}
	}

	/// <summary>
	/// JobExtension class to add automatically utility methods
	/// </summary>
	public static class JobExtension
	{
		/// <summary>
		/// Schedule this Job
		/// </summary>
		public static JobHandle Schedule(this AJob job, JobHandleCollection dependency = null )
		{
			return JobManager.ScheduleJob( job, dependency );
		}
	}
}
