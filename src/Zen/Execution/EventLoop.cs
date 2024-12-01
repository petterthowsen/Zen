using System.Collections.Concurrent;
using Zen.Common;
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
    private int _taskCount;
    private readonly List<Task> _pendingTasks;

    public EventLoop()
    {
        _taskQueue = new ConcurrentQueue<Action>();
        _cts = new CancellationTokenSource();
        _eventLoopThread = new Thread(RunEventLoop);
        _isRunning = false;
        _taskCount = 0;
        _pendingTasks = new List<Task>();
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
        Interlocked.Increment(ref _taskCount);
        _taskQueue.Enqueue(() =>
        {
            try
            {
                task();
            }
            catch (Exception ex) {
                // Log or handle the error
                Logger.Instance.Error($"Error in event loop task: {ex}");
            }
            finally
            {
                Interlocked.Decrement(ref _taskCount);
            }
        });
    }

    public void EnqueueTask(Func<Task> asyncTask)
    {
        Interlocked.Increment(ref _taskCount);
        var task = Task.Run(async () =>
        {
            try
            {
                await asyncTask();
            }
            catch (Exception ex) {
                // Log or handle the error
                Logger.Instance.Error($"Error in event loop task: {ex}");
            }
            finally
            {
                Interlocked.Decrement(ref _taskCount);
            }
        });
        lock (_pendingTasks)
        {
            _pendingTasks.Add(task);
            task.ContinueWith(t =>
            {
                lock (_pendingTasks)
                {
                    _pendingTasks.Remove(t);
                }
            });
        }
    }

    public bool HasPendingTasks
    {
        get
        {
            lock (_pendingTasks)
            {
                return _taskCount > 0 || _pendingTasks.Count > 0;
            }
        }
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
                    Logger.Instance.Error($"Error in event loop task: {ex}");
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
    /// Schedules a function to run on the event loop and returns a Promise that will be resolved
    /// when the function completes.
    /// </summary>
    public ZenPromise Schedule(Action<ZenPromise> action, Environment environment, ZenType resultType)
    {
        var promise = new ZenPromise(environment, resultType);

        // Schedule the function to run on the event loop
        EnqueueTask(() =>
        {
            try
            {
                action(promise);
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex.Message);
                promise.Reject(new ZenValue(ZenType.String, ex.Message));
            }
        });

        return promise;
    }
}
