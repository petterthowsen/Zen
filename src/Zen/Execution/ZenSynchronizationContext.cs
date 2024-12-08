using System.Collections.Concurrent;
using Zen.Common;

namespace Zen.Execution;

/// <summary>
/// A custom SynchronizationContext that ensures all async operations in Zen
/// execute on a single thread. This provides thread safety without locks
/// by queuing all work to be executed sequentially.
/// </summary>
public class ZenSynchronizationContext : SynchronizationContext
{
    private readonly BlockingCollection<(SendOrPostCallback, object?)> _queue = 
        new BlockingCollection<(SendOrPostCallback, object?)>();

    private bool _isRunning = true;
    private Exception? _lastError;

    /// <summary>
    /// Returns true if there are any pending tasks in the queue.
    /// </summary>
    public bool HasPendingWork => !_queue.IsCompleted && _queue.Count > 0;

    /// <summary>
    /// Posts a callback to be executed asynchronously on the Zen thread.
    /// </summary>
    public override void Post(SendOrPostCallback d, object? state)
    {
        _queue.Add((d, state));
    }

    /// <summary>
    /// Executes a callback synchronously on the current thread.
    /// </summary>
    public override void Send(SendOrPostCallback d, object? state) => d(state);

    /// <summary>
    /// Runs the event loop on the current thread.
    /// It keeps running as long as there is work in the queue.
    /// If Stop() is called, the loop exits.
    /// </summary>
    public void RunOnCurrentThread()
    {
        while (_isRunning)
        {
            if (_queue.TryTake(out var workItem))
            {
                var (callback, state) = workItem;
                callback(state);
    
                if (_lastError != null)
                {
                    var error = _lastError;
                    _lastError = null;
                    Logger.Instance.Error($"EVENT LOOP THREW ERROR {error}");
                    throw error;
                }
            }
            else
            {
                Logger.Instance.Debug("No work to do. Exiting event loop.");
                Stop();
            }
        }
    }

    /// <summary>
    /// Stops the event loop and prevents new work from being queued.
    /// </summary>
    public void Stop()
    {
        Logger.Instance.Debug("Stopping event loop.");
        _isRunning = false;
    }

    public void Fail(Exception e)
    {
        Logger.Instance.Error($"Fail() called. setting _lastError = e");
        // Set the exception as the last error
        _lastError = e;
    }
}
