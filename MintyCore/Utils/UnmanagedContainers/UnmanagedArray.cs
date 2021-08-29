using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MintyCore.Utils.UnmanagedContainers
{
    public unsafe struct UnmanagedArray<TItem> : IEnumerable<TItem> where TItem : unmanaged
    {
        public int Length { get; init; }
        private readonly TItem* items;
        private readonly UnmanagedDisposer<TItem> _disposer;

        public UnmanagedArray(int length, bool clearValues = true)
        {
            Length = length;
            items = (TItem*)AllocationHandler.Malloc<TItem>(length);
            _disposer = new UnmanagedDisposer<TItem>(&DisposeItems, items);

            if (clearValues)
            {
                for (int i = 0; i < length; i++)
                {
                    items[i] = default;
                }
            }
        }

        public IEnumerator<TItem> GetEnumerator()
        {
            return new Enumerator(Length, items);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Increase the internal reference counter. For each reference the dispose method needs to be called once.
        /// </summary>
        public void IncreaseRefCount()
        {
            _disposer.IncreaseRefCount();
        }

        public void DecreaseRefCount()
        {
            _disposer.DecreaseRefCount();
        }

        private static void DisposeItems(TItem* items)
        {
            AllocationHandler.Free(new IntPtr(items));
        }

        public TItem this[int index]
        {
            get
            {
                if (index < 0 || index >= Length) throw new ArgumentOutOfRangeException();
                return items[index];
            }
            set
            {
                if (index < 0 || index >= Length) throw new ArgumentOutOfRangeException();
                items[index] = value;
            }
        }

        private struct Enumerator : IEnumerator<TItem>
        {
            int _length;
            TItem* _items;
            int _index;

            public Enumerator(int length, TItem* items)
            {
                _length = length;
                _items = items;
                _index = -1;
            }

            public TItem Current => _items[_index];

            object IEnumerator.Current => Current;

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                _index++;
                return _index < _length;
            }

            public void Reset()
            {
                throw new NotSupportedException();
            }
        }
    }
}