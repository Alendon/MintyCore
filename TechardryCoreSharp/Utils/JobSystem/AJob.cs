using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechardryCoreSharp.Utils.JobSystem
{
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

	public static class JobExtension
	{
		public static JobHandle Schedule(this AJob job, JobHandleCollection dependency = null )
		{
			return JobManager.ScheduleJob( job, dependency );
		}
	}
}
