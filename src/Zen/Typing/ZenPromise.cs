using System.Threading.Tasks;
using Zen.Common;
using Zen.Execution;
using Environment = Zen.Execution.Environment;

namespace Zen.Typing;

/// <summary>
/// Represents a Promise in Zen, similar to JavaScript Promises.
/// Built on top of .NET's Task infrastructure.
/// </summary>
public class ZenPromise
{
    private readonly TaskCompletionSource<ZenValue> _tcs;
    private readonly Environment _environment;
    private readonly ZenType _resultType;
    private readonly List<Action> _thenCallbacks = new();
    private readonly List<Action> _catchCallbacks = new();
    private bool _isResolved;
    private bool _isRejected;

    public ZenPromise(Environment environment, ZenType resultType)
    {
        _tcs = new TaskCompletionSource<ZenValue>();
        _environment = environment;
        _resultType = resultType;
        _isResolved = false;
        _isRejected = false;
    }

    public void Resolve(ZenValue value)
    {
        if (_isResolved || _isRejected)
        {
            throw new RuntimeError("Cannot resolve a promise that has already been resolved or rejected");
        }

        if (!TypeChecker.IsCompatible(value.Type, _resultType))
        {
            throw new RuntimeError($"Cannot resolve promise with value of type '{value.Type}' when '{_resultType}' was expected");
        }

        _isResolved = true;
        _tcs.SetResult(value);
        
        foreach (var callback in _thenCallbacks)
        {
            callback();
        }
    }

    public void Reject(ZenValue error)
    {
        if (_isResolved || _isRejected)
        {
            throw new RuntimeError("Cannot reject a promise that has already been resolved or rejected");
        }

        _isRejected = true;
        _tcs.SetException(new Exception(error.ToString()));
        
        foreach (var callback in _catchCallbacks)
        {
            callback();
        }
    }

public ZenPromise Then(Action callback)
{
    if (_isResolved)
    {
        try
        {
            callback();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unhandled exception in Then callback: {ex.Message}");
        }
    }
    else
    {
        _thenCallbacks.Add(() =>
        {
            try
            {
                callback();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unhandled exception in Then callback: {ex.Message}");
            }
        });
    }
    return this;
}

    public ZenPromise Catch(Action callback)
    {
        if (_isRejected)
        {
            try
            {
                callback();
            }
            catch (Exception ex)
            {
                Logger.Instance.Error($"Unhandled exception in Catch callback: {ex.Message}");
            }
        }
        else
        {
            _catchCallbacks.Add(() =>
            {
                try
                {
                    callback();
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error($"Unhandled exception in Catch callback: {ex.Message}");
                }
            });
        }
        return this;
    }

    public Task<ZenValue> AsTask()
    {
        return _tcs.Task;
    }

    public bool IsCompleted => _tcs.Task.IsCompleted;
    public bool IsResolved => _isResolved;
    public bool IsRejected => _isRejected;
    public ZenType ResultType => _resultType;
    public Environment Environment => _environment;

    public static ZenPromise Resolve(Environment environment, ZenValue value)
    {
        var promise = new ZenPromise(environment, value.Type);
        promise.Resolve(value);
        return promise;
    }

    public static ZenPromise Reject(Environment environment, ZenValue error)
    {
        var promise = new ZenPromise(environment, ZenType.Void);
        promise.Reject(error);
        return promise;
    }
}
