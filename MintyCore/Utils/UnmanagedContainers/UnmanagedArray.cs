using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace MintyCore.Utils.UnmanagedContainers;

/// <summary>
///     Unmanaged array to use arrays in <see cref="ECS.IComponent" /> for example
/// </summary>
/// <typeparam name="TItem"></typeparam>
public readonly unsafe struct UnmanagedArray<TItem> : IEnumerable<TItem> where TItem : unmanaged
{
    /// <summary>
    /// The length of the array
    /// </summary>
    public int Length { get; }

    private readonly TItem* _items;
    private readonly UnmanagedDisposer<TItem> _disposer;

    /// <summary>
    ///     Create a new <see cref="UnmanagedArray{TItem}" />
    /// </summary>
    /// <param name="length">The length of the array</param>
    /// <param name="clearValues">Whether or not the values should be cleared</param>
    public UnmanagedArray(int length, bool clearValues = true)
    {
        Length = length;
        _items = (TItem*)AllocationHandler.Malloc<TItem>(length);
        _disposer = new UnmanagedDisposer<TItem>(&DisposeItems, _items);

        Debug.Assert(_items != null, nameof(_items) + " != null");

        if (!clearValues) return;
        for (var i = 0; i < length; i++) _items[i] = default;
    }

    /// <inheritdoc />
    public IEnumerator<TItem> GetEnumerator()
    {
        return new Enumerator(Length, _items);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// Creates a new span for this array
    /// </summary>
    /// <returns></returns>
    public Span<TItem> AsSpan()
    {
        return _items is null ? Span<TItem>.Empty : new Span<TItem>(_items, Length);
    }

    /// <summary>
    ///     Increase the internal reference counter. For each reference the <see cref="DecreaseRefCount" /> method needs to be
    ///     called once.
    /// </summary>
    public void IncreaseRefCount()
    {
        _disposer.IncreaseRefCount();
    }

    /// <summary>
    ///     Decreases the internal reference counter. The array will be disposed if the reference counter hits 0
    /// </summary>
    public bool DecreaseRefCount()
    {
        return _disposer.DecreaseRefCount();
    }

    private static void DisposeItems(TItem* items)
    {
        AllocationHandler.Free(new IntPtr(items));
    }

    /// <summary>
    ///     Access <see cref="TItem" /> at given <paramref name="index" />
    /// </summary>
    public ref TItem this[int index]
    {
        get
        {
            if (index < 0 || index >= Length) throw new ArgumentOutOfRangeException();
            return ref _items[index];
        }
    }

    /// <summary>
    ///     Access <see cref="TItem" /> at given <paramref name="index" />
    /// </summary>
    public ref TItem this[uint index]
    {
        get
        {
            if (index >= Length) throw new ArgumentOutOfRangeException();
            return ref _items[index];
        }
    }

    private struct Enumerator : IEnumerator<TItem>
    {
        private readonly int _length;
        private readonly TItem* _items;
        private int _index;

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