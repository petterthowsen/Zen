# Proposal: Introducing Asynchronous Programming to the Zen Runtime

## Overview

This proposal outlines a strategy to incorporate asynchronous programming into the Zen language runtime. The goal is to provide a simple, thread-safe concurrency model initially built on a single-threaded asynchronous execution model, with the potential to expand into multi-threading later. We aim to leverage C#’s `async/await` pattern, `Task`s, and a custom single-threaded `SynchronizationContext` to achieve seamless async integration.

## Objectives

- **Simplicity:** Start with a single-threaded concurrency model that avoids complex synchronization issues.
- **Safety:** Ensure that all Zen code executes on one thread, eliminating data races and the need for locks.
- **Future-proofing:** Implement the design so that we can later introduce multiple threads if needed.
- **Integration:** Leverage C#’s built-in async/await and the `Task` API for robust, scalable asynchronous operations.

## Approach

1. **Single-Threaded SynchronizationContext**

   - Implement a custom `SynchronizationContext` that queues all asynchronous work and executes it on a single thread.
   - All `await` continuations return to this context, ensuring that Zen code remains strictly single-threaded.
   
   **Example:**
   ```csharp
   public class ZenSynchronizationContext : SynchronizationContext
   {
       private readonly BlockingCollection<(SendOrPostCallback, object)> _queue = 
           new BlockingCollection<(SendOrPostCallback, object)>();

       public override void Post(SendOrPostCallback d, object state) => _queue.Add((d, state));

       public override void Send(SendOrPostCallback d, object state) => d(state);

       public void RunOnCurrentThread()
       {
           while (true)
           {
               var (callback, state) = _queue.Take();
               callback(state);
           }
       }
   }
   ```

   We will set this synchronization context at the start of the Zen program:
   ```csharp
   var syncContext = new ZenSynchronizationContext();
    SynchronizationContext.SetSynchronizationContext(syncContext);
    // Optionally run syncContext.RunOnCurrentThread() on the main thread
    ```

2. **Async Interpreter Methods**

    Make the Zen interpreter’s evaluation methods async so they can await asynchronous operations.
    Extend the AST to include constructs like AwaitExpr.
    When the interpreter encounters an await, it:
    Evaluates the awaited expression to a Task.
    Uses await on that Task.
    After completion, continues execution on the same single-threaded context.

    Pseudocode:
    ```csharp
    public async Task<ZenValue> EvaluateAsync(Node node)
    {
        switch (node)
        {
            case Await awaitExpr:
                var taskVal = await EvaluateAsync(awaitExpr.Expression);
                var task = taskVal.ToTask();
                await task; // execution pauses, then resumes on the same thread
                return taskVal.GetResult();
            // ... other cases ...
        }
    }
    ```

    We can start by simply allowing only function calls to be awaited.

3. **Async Operations in Zen**

    Zen async functions can return Task or a Zen-specific promise type internally.
    External I/O and other asynchronous operations (like HTTP requests) can be done using HttpClient or other async APIs, and awaited directly in Zen via wrappers that produce a Task.

    Example:
    ```csharp
    async function fetchData(url: string): string {
        let response = await HttpGet(url); // HttpGet returns a Task<string>
        return response;
    }
    ```
    Under the hood, HttpGet(url) is a C# async method returning a Task<string>. The Zen await translates to C# await under the runtime’s interpreter logic.