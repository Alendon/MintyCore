using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TechardryCoreSharp.Utils.JobSystem
{
	public class JobHandleCollection
	{
		private HashSet<JobHandle> _collection;

		public JobHandleCollection( params JobHandle[] handles )
		{
			_collection = new HashSet<JobHandle>( handles );
		}

		public void AddJobHandle( JobHandle jobHandle )
		{
			_collection.Add( jobHandle );
		}

		public void Merge( JobHandleCollection collection )
		{
			_collection.UnionWith( collection._collection );
		}

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

		public bool Completed()
		{
			return _collection.All( x => x.Completed() );
		}
	}

	public class JobHandle
	{
		private JobHandleCollection _dependency;
		private object _lock;
		private bool _completed;

		public JobHandle( JobHandleCollection dependency = null )
		{
			_dependency = dependency;
			_lock = new object();
			_completed = false;
		}

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

	}
}
