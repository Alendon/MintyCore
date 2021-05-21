using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechardryCoreSharp.Utils.JobSystem
{
	public abstract class AParallelJob : AJob
	{
		private int _batchSize;
		 //The iteration count of batches => 100 iterations with a batchSize of 20 => iterationCount = 5
		private int _iterationCount;
		 
		private int _jobIterationCount;
		private int _jobIterationIndex;
		private int _jobIterationsRunning;

		bool[] _iterationsCompleted;
		
		object _lock;

		public AParallelJob(int batchSize, int iterations)
		{
			_batchSize = batchSize;

			_jobIterationCount = iterations / batchSize;
			_jobIterationCount += iterations % batchSize == 0 ? 0 : 1;

			_iterationCount = iterations;
			_jobIterationIndex = 0;
			_jobIterationsRunning = 0;

			_iterationsCompleted = new bool[_jobIterationCount];
			_lock = new object();

			CompletedAfterExecute = false;
		}

		public override bool PrepareJob()
		{
			lock ( _lock )
			{
				_jobIterationsRunning++;
				if(_jobIterationsRunning >= _jobIterationCount )
				{
					return true;
				}
				return false;
			}
		}

		public override void Execute()
		{
			int currentJobIteration;
			lock ( _lock )
			{
				currentJobIteration = _jobIterationIndex;
				_jobIterationIndex++;
				if ( currentJobIteration >= _iterationCount )
				{
					//return;
				}
			}

			if(currentJobIteration == 9 )
			{

			}

			int currentIterationIndex = currentJobIteration * _batchSize;
			int iterationEnd = currentIterationIndex + _batchSize <= _iterationCount ? currentIterationIndex + _batchSize : _iterationCount;

			for(var i = currentIterationIndex; i < iterationEnd; i++ )
			{
				Execute( i );
			}

			lock ( _lock )
			{
				_iterationsCompleted[currentJobIteration] = true;
				if(_iterationsCompleted.All(x=> x == true ) )
				{
					CompletedAfterExecute = true;
				}
			}
		}

		public abstract void Execute( int index );

	}
}
