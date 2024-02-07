using System;
using JetBrains.Annotations;

namespace MintyCore.Utils;

/// <summary>
/// Helper class to allocate and track memory
/// </summary>
[PublicAPI]
public interface IAllocationHandler
{
    /// <summary>
    ///     Malloc memory block with the given size
    /// </summary>
    /// <param name="size"></param>
    /// <returns></returns>
    IntPtr Malloc(int size);

    /// <summary>
    ///     Malloc memory block with the given size
    /// </summary>
    /// <param name="size"></param>
    /// <returns></returns>
    IntPtr Malloc(IntPtr size);

    /// <summary>
    ///     Malloc a memory block for <paramref name="count" /> <typeparamref name="T" />
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="count"></param>
    /// <returns></returns>
    IntPtr Malloc<T>(int count = 1) where T : unmanaged;

    /// <summary>
    /// Allocate a span of <paramref name="count" /> <typeparamref name="T" />
    /// </summary>
    /// <param name="count"> The amount of <typeparamref name="T" /> to allocate </param>
    /// <typeparam name="T"> The type to allocate </typeparam>
    /// <returns> The allocated span </returns>
    public Span<T> MallocSpan<T>(int count = 1) where T : unmanaged;

    /// <summary>
    ///   Free a memory block
    /// </summary>
    /// <param name="span"> The span to free </param>
    /// <typeparam name="T"> The type of the span </typeparam>
    public void Free<T>(Span<T> span) where T : unmanaged;

    /// <summary>
    ///     Free an allocation
    /// </summary>
    /// <param name="allocation"></param>
    void Free(IntPtr allocation);

    /// <summary>
    ///     Check if an allocation is still valid (not freed)
    /// </summary>
    /// <param name="allocation"></param>
    /// <returns></returns>
    bool AllocationValid(IntPtr allocation);

    /// <summary>
    ///    Track a managed allocation
    /// </summary>
    /// <param name="obj"> The object to track </param>
    void TrackAllocation(object obj);

    /// <summary>
    ///  Remove a managed allocation from tracking
    /// </summary>
    /// <param name="obj"> The object to remove from tracking </param>
    void RemoveAllocation(object obj);

    /// <summary>
    ///   Check for leaks in the given <see cref="ModState" />
    /// </summary>
    void CheckForLeaks(ModState stateToCheck);
}