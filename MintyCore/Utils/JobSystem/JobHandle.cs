using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MintyCore.Utils.JobSystem
{
	/// <summary>
	/// Collection of JobHandles. Used to check if a all containing jobs are completed or to wait for them
	/// </summary>
	public class JobHandleCollection
	{
		private HashSet<JobHandle> _collection;

		/// <summary>
		/// Instantiate a new <see cref="JobHandleCollection"/>
		/// </summary>
		/// <param name="handles"></param>
		public JobHandleCollection( params JobHandle[] handles )
		{
			_collection = new HashSet<JobHandle>( handles );
		}

		/// <summary>
		/// Add a JobHandle to the Collection
		/// </summary>
		/// <param name="jobHandle"></param>
		public void AddJobHandle( JobHandle jobHandle )
		{
			_collection.Add( jobHandle );
		}

		/// <summary>
		/// Merge the current <see cref="JobHandleCollection"/> with another
		/// </summary>
		/// <param name="collection"></param>
		public void Merge( JobHandleCollection collection )
		{
			_collection.UnionWith( collection._collection );
		}

		/// <summary>
		/// Wait for the completion of all <see cref="JobHandle"/>
		/// </summary>
		public void Complete()
		{
			if ( JobManager.GetJobHandleThreadsID().Contains( Thread.CurrentThread.ManagedThreadId ) )
			{
				throw new Exception( "JobHandleCollection.Complete() can not be called from a job thread to prevent blocking" );
			}
			foreach ( var entry in _collection )
			{
				entry.Complete();
			}
		}

		/// <summary>
		/// Check if all <see cref="JobHandle"/> are completed
		/// </summary>
		public bool Completed()
		{
			return _collection.All( x => x.Completed() );
		}
	}

	/// <summary>
	/// Handle to check if a job is completed or to wait for the completion
	/// </summary>
	public class JobHandle
	{
		private JobHandleCollection _dependency;
		private object _lock;
		private volatile bool _completed;

		/// <summary>
		/// Create a new JobHandle
		/// </summary>
		/// <param name="dependency"></param>
		public JobHandle( JobHandleCollection dependency = null )
		{
			_dependency = dependency;
			_lock = new object();
			_completed = false;
		}

		/// <summary>
		/// Wait for the Completion of the Job
		/// </summary>
		public void Complete()
		{
			if ( JobManager.GetJobHandleThreadsID().Contains( Thread.CurrentThread.ManagedThreadId ) )
			{
				throw new Exception( "JobHandle.Complete() can not be called from a job thread to prevent blocking" );
			}
			try
			{
				Monitor.Enter( _lock );
				while ( !_completed )
				{
					Monitor.Wait( _lock, 50 );
				}
			}
			finally
			{
				Monitor.Exit( _lock );
			}
		}

		/// <summary>
		/// Check if the Job is completed
		/// </summary>
		/// <returns></returns>
		public bool Completed()
		{
			try
			{
				Monitor.Enter( _lock );
				return _completed;
			}
			finally
			{
				Monitor.Exit( _lock );
			}
		}

		internal bool DependencyCompleted()
		{
			if ( _dependency is not null )
			{
				return _dependency.Completed();
			}
			return true;
		}

		/// <summary>
		/// Mark the JobHandle as completed. If you dont know what you're doing don't call this
		/// </summary>
		internal void MarkAsCompleted()
		{
			try
			{
				Monitor.Enter( _lock );
				_completed = true;
				Monitor.PulseAll( _lock );
			}
			finally
			{
				Monitor.Exit( _lock );
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="jobHandle"></param>
		public static implicit operator JobHandleCollection(JobHandle jobHandle )
		{
			return new JobHandleCollection( jobHandle );
		}

	}
}
