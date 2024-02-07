using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MintyCore.Graphics.VulkanObjects;
using MintyCore.Utils;
using MintyCore.Utils.Maths;
using Silk.NET.Vulkan;

namespace MintyCore.Graphics.Utils;

/// <summary>
/// A helper class to wait for fences asynchronously
/// </summary>
[Singleton<IAsyncFenceAwaiter>(SingletonContextFlags.NoHeadless)]
public class AsyncFenceAwaiter : IAsyncFenceAwaiter
{
    private Thread? _thread;
    private volatile bool _running;

    private readonly Dictionary<ManagedFence, (TaskCompletionSource<bool> tcs, DateTime timeout,
        CancellationToken cancellationToken)> _awaiters =
        new();

    private readonly ManualResetEvent _newAwaiterMutex = new(false);

    public required IVulkanEngine VulkanEngine { private get; init; }

    /// <summary>
    /// Wait for a fence asynchronously to be signaled
    /// </summary>
    /// <param name="fence"> The fence to wait for </param>
    /// <param name="timeout"> The timeout in milliseconds </param>
    /// <param name="cancellationToken"> The cancellation token </param>
    /// <returns> True if the fence was signaled, false if the timeout was reached </returns>
    [PublicAPI]
    public Task<bool> AwaitAsync(ManagedFence fence, uint timeout = uint.MaxValue,
        CancellationToken cancellationToken = default)
    {
        if (!_running) throw new MintyCoreException("AsyncFenceAwaiter is not running");

        lock (_awaiters)
        {
            if (_awaiters.TryGetValue(fence, out var awaiter))
            {
                return awaiter.tcs.Task;
            }


            var tcs = new TaskCompletionSource<bool>();
            _awaiters.Add(fence, (tcs, DateTime.UtcNow.AddMilliseconds(timeout), cancellationToken));

            _newAwaiterMutex.Set();


            return tcs.Task;
        }
    }

    private void Worker()
    {
        var fences = Span<Fence>.Empty;

        while (_running)
        {
            bool isEmpty;

            lock (_awaiters)
            {
                isEmpty = _awaiters.Count == 0;
            }

            if (isEmpty)
            {
                _newAwaiterMutex.WaitOne();
                continue;
            }

            GetActiveFences(ref fences, out var fenceCount);

            if (fenceCount == 0)
                continue;

            const ulong waitTime = 2500 * 1_000_000ul;

            var result =
                VulkanEngine.Vk.WaitForFences(VulkanEngine.Device, fences.Slice(0, fenceCount), false, waitTime);

            if (result == Result.Timeout)
                continue;

            if (result != Result.Success)
            {
                VulkanUtils.Assert(result);
            }

            MarkFencesCompleted();
        }
    }

    private void MarkFencesCompleted()
    {
        lock (_awaiters)
        {
            foreach (var managedFence in _awaiters.Keys.ToArray())
            {
                if (!managedFence.IsSignaled()) continue;

                if (!_awaiters.Remove(managedFence, out var awaiter))
                    continue;

                awaiter.tcs.SetResult(true);
            }
        }
    }

    private void GetActiveFences(ref Span<Fence> fences, out int fenceCount)
    {
        fenceCount = 0;
        lock (_awaiters)
        {
            if (fences.Length < _awaiters.Count)
                fences = new Fence[MathHelper.CeilPower2(_awaiters.Count)];

            foreach (var managedFence in _awaiters.Keys.ToArray())
            {
                var (tcs, timeout, cancellationToken) = _awaiters[managedFence];

                if (cancellationToken.IsCancellationRequested)
                {
                    _awaiters.Remove(managedFence);
                    tcs.SetCanceled();
                    continue;
                }

                if (DateTime.UtcNow > timeout)
                {
                    _awaiters.Remove(managedFence);
                    tcs.SetResult(false);
                    continue;
                }

                fences[fenceCount++] = managedFence.Fence;
            }
        }
    }

    /// <summary>
    /// Start the async fence awaiter
    /// </summary>
    [PublicAPI]
    public void Start()
    {
        if (_running)
            return;

        _running = true;
        _thread = new Thread(Worker);
        _thread.Start();
    }

    /// <summary>
    /// Stop the async fence awaiter
    /// </summary>
    [PublicAPI]
    public void Stop()
    {
        if (!_running)
            return;

        _running = false;
        _newAwaiterMutex.Set();
        _thread?.Join();
        _thread = null;
    }
}