using System.Threading;
using JetBrains.Annotations;
using Silk.NET.Vulkan;
using Semaphore = System.Threading.Semaphore;

namespace MintyCore.Graphics.Render;

[PublicAPI]
public abstract class IManualAsyncWorker
{
    private volatile bool _shouldStop;
    protected CommandBuffer CommandBuffer { get; private set; }

    private Semaphore? IterationCompleteSemaphore;
    private Semaphore NextIterationSemaphore = new(0, 1);
    private Thread? _workerThread;

    public void NextIteration(CommandBuffer commandBuffer)
    {
        CommandBuffer = commandBuffer;
        NextIterationSemaphore.Release();
    }

    private void WorkerLoop()
    {
        while (true)
        {
            NextIterationSemaphore.WaitOne();
            if (_shouldStop)
                break;

            Update();
            IterationCompleteSemaphore?.Release();
        }
    }

    protected abstract void Update();

    public void Start(Semaphore iterationCompleteSemaphore)
    {
        IterationCompleteSemaphore = iterationCompleteSemaphore;
        _shouldStop = false;

        _workerThread = new Thread(WorkerLoop);
        _workerThread.Start();
    }


    public void Stop()
    {
        _shouldStop = true;
        NextIterationSemaphore.Release();
        _workerThread?.Join();
    }
}