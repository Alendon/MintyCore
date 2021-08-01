using System.Linq;

namespace MintyCore.Utils.JobSystem
{
	/// <summary>
	/// Base class to implement jobs that runs in parallel
	/// </summary>
	public abstract class AParallelJob : AJob
	{
		private int _batchSize;

		//The iteration count of batches => 100 iterations with a batchSize of 20 => iterationCount = 5
		private int _iterationCount;

		private int _jobIterationCount;
		private int _jobIterationIndex;
		private int _jobIterationsRunning;

		private bool[] _iterationsCompleted;

		private object _lock;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="batchSize"></param>
		/// <param name="iterations"></param>
		public AParallelJob( int batchSize, int iterations )
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

		/// <inheritdoc/>
		public override bool PrepareJob()
		{
			lock ( _lock )
			{
				_jobIterationsRunning++;
				if ( _jobIterationsRunning >= _jobIterationCount )
				{
					return true;
				}
				return false;
			}
		}

		/// <inheritdoc/>
		public override void Execute()
		{
			int currentJobIteration;
			lock ( _lock )
			{
				currentJobIteration = _jobIterationIndex;
				_jobIterationIndex++;
			}

			int currentIterationIndex = currentJobIteration * _batchSize;
			int iterationEnd = currentIterationIndex + _batchSize <= _iterationCount ? currentIterationIndex + _batchSize : _iterationCount;

			for ( var i = currentIterationIndex; i < iterationEnd; i++ )
			{
				Execute( i );
			}

			lock ( _lock )
			{
				_iterationsCompleted[currentJobIteration] = true;
				if ( _iterationsCompleted.All( x => x == true ) )
				{
					CompletedAfterExecute = true;
				}
			}
		}

		/// <summary>
		/// Execute the job with the current index
		/// </summary>
		public abstract void Execute( int index );
	}
}