using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MintyCore.Utils
{
	class DeletionQueue
	{
		Stack<Action> _deleteActions = new Stack<Action>();
		internal void AddDeleteAction( Action deleteAction )
		{
			_deleteActions.Push( deleteAction );
		}
		internal void Flush()
		{
			while ( _deleteActions.Count > 0 )
			{
				_deleteActions.Pop()();
			}
		}
	}
}
