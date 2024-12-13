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
    private int _pendingContinuations = 0;

    /// <summary>
    /// Returns true if there are any pending tasks in the queue or pending continuations.
    /// </summary>
    public bool HasPendingWork => !_queue.IsCompleted && (_queue.Count > 0 || _pendingContinuations > 0);

    /// <summary>
    /// Posts a callback to be executed asynchronously on the Zen thread.
    /// </summary>
    public override void Post(SendOrPostCallback d, object? state)
    {
        _queue.Add((d, state));
        Logger.Instance.Debug($"Queued work item. Pending: {_queue.Count}");
    }

    /// <summary>
    /// Executes a callback synchronously on the current thread.
    /// </summary>
    public override void Send(SendOrPostCallback d, object? state) => d(state);

    /// <summary>
    ///   Tracks a continuation that will be added to a task.
    ///   This must be called, along with CompleteContinution()
    ///   to ensure the event loop does not preemptively exit.
    /// </summary>
    /// <remarks>
    ///    This is used for example in Interpreter.CallUserFunction for top-level async function calls that fail.
    ///    It adds a continuation that calls SyncContext.Fail with the exception.
    ///    But also, importantly, calls TrackContinuation.
    /// </remarks>
    public void TrackContinuation()
    {
        Interlocked.Increment(ref _pendingContinuations);
        Logger.Instance.Debug($"Tracking continuation. Pending: {_pendingContinuations}");
    }

    /// <summary>
    /// Marks a continuation as completed
    /// </summary>
    public void CompleteContinuation()
    {
        Interlocked.Decrement(ref _pendingContinuations);
        Logger.Instance.Debug($"Completed continuation. Pending: {_pendingContinuations}");
    }

    /// <summary>
    /// Runs the event loop on the current thread.
    /// It keeps running as long as there is work in the queue or pending continuations.
    /// If Stop() is called, the loop exits.
    /// </summary>
    public void RunOnCurrentThread()
    {        
        _isRunning = true;

        while (_isRunning)
        {
            // Check for errors
            if (_lastError != null)
            {
                var error = _lastError;
                _lastError = null;
                Stop();
                throw error;
            }

            if (_queue.TryTake(out var workItem))
            {
                try 
                {
                    var (callback, state) = workItem;
                    callback(state);
                }
                catch (Exception e)
                {
                    Fail(e);
                }
            }
            else if (_pendingContinuations == 0)
            {
                // Only exit if no work AND no pending continuations
                Logger.Instance.Debug("No work to do and no pending continuations. Exiting event loop.");
                Stop();
            }
            else
            {
                // We have pending continuations but no work items, wait a bit
                Thread.Sleep(1);
            }
        }

        Logger.Instance.Debug("Stopping event loop.");
    }

    /// <summary>
    /// Stops the event loop and prevents new work from being queued.
    /// </summary>
    public void Stop()
    {
        _isRunning = false;
    }

    /// <summary>
    /// Causes the event loop to throw an exception.
    /// </summary>
    /// <param name="e"></param>
    public void Fail(Exception e)
    {
        Logger.Instance.Error($"Fail() called. setting _lastError = e");
        _lastError = e;
    }
}
