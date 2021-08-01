using System;
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

		private static List<KeyValuePair<AJob, JobHandle>> _jobs = new List<KeyValuePair<AJob, JobHandle>>();

		internal static void Start()
		{
			int threadCount = Environment.ProcessorCount;
			_threads = new Thread[threadCount];
			JobHandleThreadIDs = new HashSet<int>( threadCount );

			for ( var i = 0; i < threadCount; i++ )
			{
				Thread thread = new Thread( ThreadExecute );
				thread.Start();
				_threads[i] = thread;
				JobHandleThreadIDs.Add( thread.ManagedThreadId );
			}
		}

		internal static JobHandle ScheduleJob( AJob job, JobHandleCollection dependency )
		{
			lock ( _stopLock )
			{
				if ( _stop )
				{
					throw new InvalidOperationException( "Job scheduling is not allowed after the JobManager was stopped" );
				}
			}

			JobHandle jobHandle = new JobHandle( dependency );
			lock ( _jobs )
			{
				_jobs.Add( new KeyValuePair<AJob, JobHandle>( job, jobHandle ) );
			}

			NotifyAll();

			return jobHandle;
		}

		internal static void Stop()
		{
			lock ( _stopLock )
			{
				_stop = true;
			}
			NotifyAll();

			foreach ( var thread in _threads )
			{
				thread.Join();
			}
		}

		private static bool ShouldStop()
		{
			lock ( _stopLock )
				lock ( _jobs )
				{
					return _stop && _jobs.Count == 0;
				}
		}

		private static void ThreadExecute()
		{
			while ( !ShouldStop() )
			{
				KeyValuePair<AJob, JobHandle> jobEntry;
				if ( GetAvailableJob( out jobEntry ) )
				{
					jobEntry.Key.Execute();
					if ( jobEntry.Key.CompletedAfterExecute )
					{
						jobEntry.Value.MarkAsCompleted();
						NotifyAll();
					}
				}
			}
		}

		private static void NotifyAll()
		{
			try
			{
				Monitor.Enter( _syncLock );
				Monitor.PulseAll( _syncLock );
			}
			finally
			{
				Monitor.Exit( _syncLock );
			}
		}

		private static bool TryGetJobEntry( out KeyValuePair<AJob, JobHandle> jobEntry )
		{
			lock ( _jobs )
			{
				bool found = false;
				bool remove = false;
				jobEntry = default;

				foreach ( var entry in _jobs )
				{
					if ( entry.Value.DependencyCompleted() )
					{
						jobEntry = entry;
						found = true;
						remove = entry.Key.PrepareJob();

						break;
					}
				}
				if ( found )
				{
					if ( remove )
					{
						_jobs.Remove( jobEntry );
					}
					return true;
				}
				return false;
			}
		}

		private static bool GetAvailableJob( out KeyValuePair<AJob, JobHandle> jobEntry )
		{
			while ( true )
			{
				if ( TryGetJobEntry( out jobEntry ) )
				{
					return true;
				}

				if ( ShouldStop() )
					return false;

				try
				{
					Monitor.Enter( _syncLock );
					Monitor.Wait( _syncLock );
				}
				finally
				{
					Monitor.Exit( _syncLock );
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
