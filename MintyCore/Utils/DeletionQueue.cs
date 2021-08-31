using System;
using System.Collections.Generic;

namespace MintyCore.Utils
{
    internal class DeletionQueue
    {
        private readonly Stack<Action> _deleteActions = new();

        internal void AddDeleteAction(Action deleteAction)
        {
            _deleteActions.Push(deleteAction);
        }

        internal void Flush()
        {
            while (_deleteActions.Count > 0) _deleteActions.Pop()();
        }
    }
}