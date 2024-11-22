using Zen.Common;
using Zen.Typing;

namespace Zen.Execution.Async;

public enum PromiseState
{
    Pending,
    Fulfilled,
    Rejected
}

public class Promise
{
    private PromiseState _state = PromiseState.Pending;
    private ZenValue? _value;
    private string? _error;
    private readonly List<Action<ZenValue>> _thenCallbacks = new();
    private readonly List<Action<string>> _catchCallbacks = new();
    private readonly List<Action> _finallyCallbacks = new();

    public PromiseState State => _state;
    public bool IsPending => _state == PromiseState.Pending;
    public bool IsFulfilled => _state == PromiseState.Fulfilled;
    public bool IsRejected => _state == PromiseState.Rejected;

    public Promise(Action<Action<ZenValue>, Action<string>> executor)
    {
        try
        {
            executor(Resolve, Reject);
        }
        catch (Exception ex)
        {
            Reject(ex.Message);
        }
    }

    private void Resolve(ZenValue value)
    {
        if (!IsPending) return;

        _state = PromiseState.Fulfilled;
        _value = value;

        foreach (var callback in _thenCallbacks)
        {
            EventLoop.Instance.EnqueueMicrotask(() => callback(value));
        }

        foreach (var callback in _finallyCallbacks)
        {
            EventLoop.Instance.EnqueueMicrotask(callback);
        }

        _thenCallbacks.Clear();
        _catchCallbacks.Clear();
        _finallyCallbacks.Clear();
    }

    private void Reject(string error)
    {
        if (!IsPending) return;

        _state = PromiseState.Rejected;
        _error = error;

        foreach (var callback in _catchCallbacks)
        {
            EventLoop.Instance.EnqueueMicrotask(() => callback(error));
        }

        foreach (var callback in _finallyCallbacks)
        {
            EventLoop.Instance.EnqueueMicrotask(callback);
        }

        _thenCallbacks.Clear();
        _catchCallbacks.Clear();
        _finallyCallbacks.Clear();
    }

    public Promise Then(Action<ZenValue> onFulfilled)
    {
        return new Promise((resolve, reject) =>
        {
            var callback = new Action<ZenValue>(value =>
            {
                try
                {
                    onFulfilled(value);
                    resolve(value);
                }
                catch (Exception ex)
                {
                    reject(ex.Message);
                }
            });

            if (IsFulfilled && _value != null)
            {
                EventLoop.Instance.EnqueueMicrotask(() => callback(_value));
            }
            else if (IsPending)
            {
                _thenCallbacks.Add(callback);
            }
        });
    }

    public Promise Catch(Action<string> onRejected)
    {
        return new Promise((resolve, reject) =>
        {
            var callback = new Action<string>(error =>
            {
                try
                {
                    onRejected(error);
                    resolve(ZenValue.Null); // Convert rejection to success
                }
                catch (Exception ex)
                {
                    reject(ex.Message);
                }
            });

            if (IsRejected && _error != null)
            {
                EventLoop.Instance.EnqueueMicrotask(() => callback(_error));
            }
            else if (IsPending)
            {
                _catchCallbacks.Add(callback);
            }
        });
    }

    public Promise Finally(Action onFinally)
    {
        if (IsPending)
        {
            _finallyCallbacks.Add(onFinally);
        }
        else
        {
            EventLoop.Instance.EnqueueMicrotask(onFinally);
        }

        return this;
    }

    public static Promise Resolve(ZenValue value)
    {
        return new Promise((resolve, _) => resolve(value));
    }

    public static Promise Reject(string error)
    {
        return new Promise((_, reject) => reject(error));
    }

    public static Promise All(IEnumerable<Promise> promises)
    {
        var promiseList = promises.ToList();
        if (promiseList.Count == 0)
        {
            return Promise.Resolve(ZenValue.Null);
        }

        return new Promise((resolve, reject) =>
        {
            var results = new ZenValue[promiseList.Count];
            var completedCount = 0;

            for (var i = 0; i < promiseList.Count; i++)
            {
                var index = i;
                promiseList[i]
                    .Then(value =>
                    {
                        results[index] = value;
                        completedCount++;
                        if (completedCount == promiseList.Count)
                        {
                            resolve(new ZenValue(ZenType.Array, results));
                        }
                    })
                    .Catch(reject);
            }
        });
    }
}
