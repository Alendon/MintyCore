using System.Threading;
using System.Threading.Tasks;
using MintyCore.Render.VulkanObjects;

namespace MintyCore.Render.Utils;

public interface IAsyncFenceAwaiter
{
    /// <summary>
    /// Wait for a fence asynchronously to be signaled
    /// </summary>
    /// <param name="fence"> The fence to wait for </param>
    /// <param name="timeout"> The timeout in milliseconds </param>
    /// <param name="cancellationToken"> The cancellation token </param>
    /// <returns> True if the fence was signaled, false if the timeout was reached </returns>
    Task<bool> AwaitAsync(ManagedFence fence, uint timeout = uint.MaxValue,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Start the async fence awaiter
    /// </summary>
    void Start();

    /// <summary>
    /// Stop the async fence awaiter
    /// </summary>
    void Stop();
}