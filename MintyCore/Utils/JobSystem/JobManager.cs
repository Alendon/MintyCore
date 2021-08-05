using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MintyCore.Utils.JobSystem
{
	/// <summary>
	/// JobManager class to execute <see cref="AJob"/>
	/// </summary>
	public static class JobManager
	{
		private static HashSet<int> JobHandleThreadIDs;

		private static Thread[] _threads;

		private static object _stopLock = new object();
		private static object _syncLock = new object();
		private static bool _stop = false;

		private static ConcurrentQueue<(AJob, JobHandle)> _jobs = new();

		internal static void Start()
		{
			int threadCount = Environment.ProcessorCount;
			_threads = new Thread[threadCount];
			JobHandleThreadIDs = new HashSet<int>(threadCount);

			for (var i = 0; i < threadCount; i++)
			{
				Thread thread = new Thread(ThreadExecute);
				thread.Start();
				_threads[i] = thread;
				JobHandleThreadIDs.Add(thread.ManagedThreadId);
			}
		}

		internal static JobHandle ScheduleJob(AJob job, JobHandleCollection dependency)
		{
			lock (_stopLock)
			{
				if (_stop)
				{
					throw new InvalidOperationException("Job scheduling is not allowed after the JobManager was stopped");
				}
			}

			JobHandle jobHandle = new JobHandle(dependency);
			_jobs.Enqueue((job, jobHandle));

			NotifyAll();

			return jobHandle;
		}

		internal static void Stop()
		{
			lock (_stopLock)
			{
				_stop = true;
			}
			NotifyAll();

			foreach (var thread in _threads)
			{
				thread.Join();
			}
		}

		private static bool ShouldStop()
		{
			lock (_stopLock)
				lock (_jobs)
				{
					return _stop && _jobs.Count == 0;
				}
		}

		private static void ThreadExecute()
		{
			while (!ShouldStop())
			{
				(AJob job, JobHandle handle) jobEntry;
				if (GetAvailableJob(out jobEntry))
				{
					jobEntry.job.Execute();
					if (jobEntry.job.CompletedAfterExecute)
					{
						jobEntry.handle.MarkAsCompleted();
						NotifyAll();
					}
				}
			}
		}

		private static void NotifyAll()
		{
			try
			{
				Monitor.Enter(_syncLock);
				Monitor.PulseAll(_syncLock);
			}
			finally
			{
				Monitor.Exit(_syncLock);
			}
		}

		private static bool TryGetJobEntry(out (AJob job, JobHandle jobHandle) jobEntry)
		{
			bool found = false;
			bool remove = false;

			if (_jobs.TryDequeue(out jobEntry))
			{
				if (jobEntry.jobHandle.DependencyCompleted())
				{
					remove = jobEntry.job.PrepareJob();
					found = true;
				}

				if (!remove)
				{
					_jobs.Enqueue(jobEntry);
				}
			}

			var firstFound = jobEntry;


			while (!found && !_jobs.IsEmpty)
			{
				if (_jobs.TryDequeue(out jobEntry))
				{
					if (jobEntry.jobHandle.DependencyCompleted())
					{
						remove = jobEntry.job.PrepareJob();
						found = true;
					}

					if (!remove)
					{
						_jobs.Enqueue(jobEntry);
					}

					//Check if iterated over the whole queue once to prevent being stuck in the loop
					if(firstFound == jobEntry)
					{
						return found;
					}
				}

			}
			return found;
		}

		private static bool GetAvailableJob(out (AJob, JobHandle) jobEntry)
		{
			while (true)
			{
				if (TryGetJobEntry(out jobEntry))
				{
					return true;
				}

				if (ShouldStop())
					return false;

				try
				{
					Monitor.Enter(_syncLock);
					Monitor.Wait(_syncLock);
				}
				finally
				{
					Monitor.Exit(_syncLock);
				}
			}
		}

		/// <summary>
		/// Get the thread Ids used by the <see cref="JobManager"/>
		/// </summary>
		/// <returns></returns>
		public static IReadOnlySet<int> GetJobHandleThreadsID()
		{
			return JobHandleThreadIDs;
		}
	}
}
