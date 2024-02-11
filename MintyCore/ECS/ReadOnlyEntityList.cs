using System;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace MintyCore.ECS;

/// <summary>
/// Read only list of entities.
/// </summary>
[PublicAPI]
public class ReadOnlyEntityList : IReadOnlyList<Entity>
{
    private readonly Entity[] _entities;
    private readonly int _count;

    /// <summary>
    /// Create a wrapper around a list of entities.
    /// </summary>
    /// <param name="entities"></param>
    /// <param name="count"></param>
    public ReadOnlyEntityList(Entity[] entities, int count)
    {
        _entities = entities;
        _count = count;
    }

    /// <inheritdoc />
    public IEnumerator<Entity> GetEnumerator()
    {
        return new Enumerator(_entities, _count);
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <inheritdoc />
    public int Count => _count;

    /// <inheritdoc />
    public Entity this[int index]
    {
        get
        {
            if (index >= _count) throw new IndexOutOfRangeException();
            return _entities[index];
        }
    }

    private class Enumerator : IEnumerator<Entity>
    {
        private readonly Entity[] _entities;
        private readonly int _count;
        private int _index = -1;

        public Enumerator(Entity[] entities, int count)
        {
            _entities = entities;
            _count = count;
        }

        public bool MoveNext()
        {
            _index++;
            return _index < _count;
        }

        public void Reset()
        {
            _index = -1;
        }

        public Entity Current => _entities[_index];

        object IEnumerator.Current => Current;

        public void Dispose()
        {
        }
    }
}