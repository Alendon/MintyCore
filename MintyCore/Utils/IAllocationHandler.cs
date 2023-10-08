using System;

namespace MintyCore.Utils;

public interface IAllocationHandler
{
    internal void CheckUnFreed();

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
    unsafe IntPtr Malloc<T>(int count = 1) where T : unmanaged;

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
}