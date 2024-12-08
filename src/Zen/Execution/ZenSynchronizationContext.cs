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
        if (!_isRunning)
        {
            throw new InvalidOperationException("Cannot post to stopped context");
        }
        _queue.Add((d, state));
    }

    /// <summary>
    /// Executes a callback synchronously on the current thread.
    /// </summary>
    public override void Send(SendOrPostCallback d, object? state) => d(state);

    /// <summary>
    /// Runs the event loop on the current thread. This method blocks until Stop() is called
    /// or an unhandled exception occurs.
    /// </summary>
    public void RunOnCurrentThread()
    {
        _isRunning = true;

        while (_isRunning)
        {
            try
            {
                // Try to take an item with no timeout (blocking)
                if (_queue.TryTake(out var workItem))
                {
                    var (callback, state) = workItem;
                    Logger.Instance.Debug($"Executing callback: {callback}");
                    callback(state);

                    if (_lastError != null)
                    {
                        var error = _lastError;
                        _lastError = null;
                        throw error;
                    }
                }
                else
                {
                    // If we fail to take any item and the queue is completed,
                    // it means no more work will arrive. If we're not running, we can stop.
                    Stop();
                }
            }
            catch (Exception ex)
            {
                _lastError = ex;
                Stop();
                throw;
            }
        }
    }

    /// <summary>
    /// Stops the event loop and prevents new work from being queued.
    /// </summary>
    public void Stop()
    {
        _isRunning = false;
        _queue.CompleteAdding();
    }

    
}