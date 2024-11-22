# Event Loop Implementation Proposal for Zen

## Overview

This document outlines the proposed implementation of Node.js-style concurrency in Zen using a single-threaded event loop model with async/await syntax.

## Architecture

### Core Components

1. **Event Loop**
   - Single-threaded execution model similar to Node.js
   - Task queue for managing async operations
   - Integration with .NET's Task infrastructure
   - Non-blocking I/O operations

2. **Promise System**
   - Promise-based async operation handling
   - Built on top of .NET's TaskCompletionSource
   - Support for chaining (.then(), .catch())
   - Integration with async/await syntax

3. **Language Features**
   - async/await keywords
   - Promise-returning functions
   - Error handling with try/catch
   - Cancellation support

## .NET Task Infrastructure

The implementation leverages .NET's robust Task Parallel Library (TPL) for several key reasons:

1. **Task<T> Class**
   - Represents asynchronous operations
   - Built-in support for continuation
   - Efficient state machine implementation
   - Automatic thread pool management

2. **TaskCompletionSource**
   - Enables manual control over task completion
   - Perfect for implementing Promise-like behavior
   - Supports both success and error scenarios
   - Thread-safe completion signaling

3. **async/await Infrastructure**
   - State machine generation
   - Efficient suspension and resumption
   - Integration with SynchronizationContext
   - Exception propagation

4. **Benefits of Using TPL**
   - Battle-tested implementation
   - High-performance execution
   - Memory-efficient operations
   - Built-in thread safety

## Implementation Steps

### Phase 1: Core Infrastructure ✅

1. **Promise Implementation** ✅
   - [x] Create ZenPromise class
   - [x] Implement resolve/reject methods
   - [x] Add then/catch chaining support
   - [x] Integrate with TaskCompletionSource

2. **Event Loop** ✅
   - [x] Create EventLoop class
   - [x] Implement task queue
   - [x] Add task scheduling mechanism
   - [x] Create main loop execution logic

3. **Parser Changes** ✅
   - [x] Add async/await keywords to lexer
   - [x] async functions (FuncStmt.Async = true)
   - [x] Create AwaitExpr AST node

4. **Interpreter Updates** ✅
   - [x] Add async function execution support
   - [x] Implement await expression handling
   - [x] Create async call stack tracking
   - [x] Add promise resolution logic

### Phase 2: Built-in Operations

1. **Async I/O Operations**
   - [ ] Implement async file operations
   - [ ] Add network I/O support
   - [x] Create timer functions (delay)
   - [ ] Add process operations

2. **Error Handling**
   - [ ] Implement async stack traces
   - [ ] Add promise rejection tracking
   - [ ] Create error propagation system
   - [ ] Add unhandled rejection handlers

3. **Promise Features**
   - [ ] Implement Promise.all
   - [ ] Add Promise.race
   - [ ] Create Promise.any
   - [ ] Add Promise.allSettled

### Phase 3: Advanced Features

1. **Optimization**
   - [ ] Add promise pooling
   - [ ] Implement task batching
   - [ ] Optimize memory usage
   - [ ] Add performance monitoring

2. **Standard Library**
   - [ ] Create async collections
   - [ ] Add async iterators
   - [ ] Implement async utilities
   - [ ] Add stream operations

3. **Developer Tools**
   - [ ] Add async debugging support
   - [ ] Create performance profiling
   - [ ] Implement memory analysis
   - [ ] Add monitoring tools

## Example Usage

```zen
// Example of async file reading
async func readFile(path: string): string {
    return await File.readAsync(path)
}

// Example of parallel operations
async func processFiles(paths: array<string>): array<string> {
    return await Promise.all(paths.map(p => readFile(p)))
}

// Example of error handling
async func safeRead(path: string): string? {
    try {
        return await readFile(path)
    } catch (error) {
        print "Error reading file: ${error}"
        return null
    }
}
```

## Technical Considerations

1. **Memory Management**
   - Proper cleanup of pending promises
   - Memory-efficient task queuing
   - Resource cleanup on cancellation

2. **Error Handling**
   - Proper async stack traces
   - Unhandled rejection tracking
   - Error boundary definition

3. **Performance**
   - Minimal overhead for async operations
   - Efficient task scheduling
   - Optimal memory usage

4. **Compatibility**
   - Integration with existing code
   - Interop with .NET async methods
   - Support for third-party async operations

## Next Steps

1. Begin implementing Phase 2:
   - Add async file operations
   - Implement Promise.all and other Promise combinators
   - Add proper error handling for async operations

2. Create documentation for:
   - Async/await usage
   - Promise API
   - Error handling best practices

3. Plan Phase 3 features based on user feedback and needs

## Conclusion

Phase 1 implementation is complete, providing Zen with modern, efficient concurrency support similar to Node.js, while leveraging the robust .NET Task infrastructure for reliable and performant execution. The next phases will focus on expanding the async capabilities with more built-in operations and advanced features.
