using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MintyCore.Utils;

namespace MintyCore.Render;

/// <inheritdoc />
/// <summary>
/// Represents a generic render input handler that allows setting data.
/// </summary>
/// <typeparam name="TKey">The type of the key used to identify the data.</typeparam>
/// <typeparam name="TValue">The type of the data to be processed.</typeparam>
public interface IRenderInputKeyValue<in TKey, in TValue> : IRenderInputKey<TKey>
{
    /// <summary>
    /// Sets the data for a given key.
    /// </summary>
    /// <param name="key">The key identifying the data.</param>
    /// <param name="value">The data to be set.</param>
    void SetData(TKey key, TValue value);
}

/// <summary>
/// Represents a render input handler that allows removing data by key.
/// </summary>
/// <typeparam name="TKey">The type of the key used to identify the data.</typeparam>
public interface IRenderInputKey<in TKey>
{
    /// <summary>
    /// Removes the data associated with a given key.
    /// </summary>
    /// <param name="key">The key identifying the data to be removed.</param>
    void RemoveData(TKey key);
}

public interface IRenderInputConcreteResult<out TConcreteResult> : IRenderInput
{
    TConcreteResult GetConcreteResulttt();
}

/// <summary>
/// Represents a non-generic render input handler that processes data.
/// </summary>
public interface IRenderInput : IDisposable
{
    /// <summary>
    /// Processes the input data.
    /// </summary>
    Task Process();

    object GetResult();

    /// <summary>
    /// Execute this <see cref="IRenderInput"/> before the given <see cref="IRenderInput"/>.
    /// </summary>
    IEnumerable<Identification> ExecuteBefore => Enumerable.Empty<Identification>();

    /// <summary>
    /// Execute this <see cref="IRenderInput"/> after the given <see cref="IRenderInput"/>.
    /// </summary>
    IEnumerable<Identification> ExecuteAfter => Enumerable.Empty<Identification>();
}