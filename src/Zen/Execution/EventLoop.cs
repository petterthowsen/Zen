using System.Collections.Concurrent;
using Zen.Typing;

namespace Zen.Execution;

/// <summary>
/// Implements a single-threaded event loop for managing async operations in Zen.
/// Similar to Node.js event loop model.
/// </summary>
public class EventLoop
{
    private readonly ConcurrentQueue<Action> _taskQueue;
    private readonly CancellationTokenSource _cts;
    private readonly Thread _eventLoopThread;
    private bool _isRunning;

    public EventLoop()
    {
        _taskQueue = new ConcurrentQueue<Action>();
        _cts = new CancellationTokenSource();
        _eventLoopThread = new Thread(RunEventLoop);
        _isRunning = false;
    }

    public void Start()
    {
        if (!_isRunning)
        {
            _isRunning = true;
            _eventLoopThread.Start();
        }
    }

    public void Stop()
    {
        if (_isRunning)
        {
            _isRunning = false;
            _cts.Cancel();
            _eventLoopThread.Join();
        }
    }

    public void EnqueueTask(Action task)
    {
        _taskQueue.Enqueue(task);
    }

    private void RunEventLoop()
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            if (_taskQueue.TryDequeue(out Action? task))
            {
                try
                {
                    task();
                }
                catch (Exception ex)
                {
                    // Log or handle the error
                    Console.WriteLine($"Error in event loop task: {ex}");
                }
            }
            else
            {
                // No tasks to process, sleep briefly to prevent CPU spinning
                Thread.Sleep(1);
            }
        }
    }

    /// <summary>
    /// Schedules a Task to run on the event loop and returns a Promise that will be resolved
    /// when the task completes.
    /// </summary>
    public ZenPromise Schedule<T>(Func<Task<T>> taskFactory, Environment environment, ZenType resultType)
    {
        var promise = new ZenPromise(environment, resultType);

        EnqueueTask(async () =>
        {
            try
            {
                var result = await taskFactory();
                promise.Resolve(new ZenValue(resultType, result));
            }
            catch (Exception ex)
            {
                promise.Reject(new ZenValue(ZenType.String, ex.Message));
            }
        });

        return promise;
    }
}
