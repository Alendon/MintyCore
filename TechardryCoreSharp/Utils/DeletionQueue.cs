using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechardryCoreSharp.Utils
{
	class DeletionQueue
	{
		Queue<Action> _deleteActions = new Queue<Action>();
		internal void AddDeleteAction( Action deleteAction )
		{
			_deleteActions.Enqueue( deleteAction );
		}
		internal void Flush()
		{
			while ( _deleteActions.Count > 0 )
			{
				_deleteActions.Dequeue()();
			}
		}
	}
}
