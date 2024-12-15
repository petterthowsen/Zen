```js
              _____                
             / _  /  ___  _ __   
             \// /  / _ \| '_ \    
              / //\|  __/| | | |   
             /____/ \___||_| |_|   

        "Write Less, Do More, Safely."

```

# About Zen

Zen is a high-level, interpreted programming language with modern features like OOP, concurrency, generics and strict typing.

See `spec.zen` to explore the syntax. Do note however that Zen is in a very early stage and mmuch is yet subject to change.

The goal of zen is to be a simple language that
- Doesn't reinvent the wheel
- Has familiar syntax
- Async/Await just like in Node JS
- Cross Platform
- Neither overly opinionated nor full of magic
- Rich standard library suitable for Web Servers & Web Apps, as well as desktop apps.

This implementation is a tree-walk interpreter and it is therefore relatively slow compared to more optimized runtimes (bytecode or especially native machine code.)

For compute-heavy or real-time-dependent projects, Zen is not suitable. For other use cases, it's totally fine - computers are fast enough these days.

For Comparison, here's a fib function in zen:

```zen
func fib(n:int) : int {
    total_n += 1

    if n <= 2 {
        return n
    }
    return fib(n - 1) + fib(n - 2)
}
```

Calling this, results in a total of 13529 recursive calls and takes about 644ms on my laptop.
Conversely, a similar script in Node JS takes about 6ms. So, It's about a hundred times slower.

## Installing

Binaries for windows, mac & linux will be made available when Zen is more mature or upon request. It's pretty easy to build it yourself - all you need is dotnet with SDK 8.0 or later.

## Building and Running

To build and run the project:

```bash
# Build the project
dotnet build

# Run the project
dotnet run --project src/Zen

# to build and run, a helper script 'zen.sh' exists which starts a REPL.
./zen.sh

# alternatively, you can pass a file
./zen.sh myZenFile.zen
```

## Building binaries
use `dotnet publish -c Release -r <runtime-identifier> --self-contained` to build binaries.
Replace runtime-identifier with one of these:
- win-x64
- linux-x64
- osx-x64

## Testing

To run the tests:

```bash
# Run all tests
dotnet test
```

## Development

The project uses:
- C# 12 features
- .NET 8.0
- XUnit for testing

To add new tests, create test files in the `tests/Zen.Tests` directory following the naming convention `*Tests.cs`.

## Project File Organization

- Each project (src/Zen and tests/Zen.Tests) follows standard .NET conventions:
  - Source code files are in the project root or organized in subdirectories
  - `bin` directory contains compiled outputs (excluded from git)
  - `obj` directory contains intermediate build files (excluded from git)

## Architecture

The Zen language implementation follows a classic compiler/interpreter architecture with four main components:

1. **Lexer** (`src/Zen/Lexing/Lexer.cs`):
   - Breaks source code into tokens (lexical analysis)
   - Handles various token types including keywords, identifiers, literals, operators
   - Supports string literals with escape sequences
   - Manages source locations for error reporting
   - Provides robust error handling for invalid tokens

2. **Parser** (`src/Zen/Parsing/Parser.cs`):
   - Converts token stream into an Abstract Syntax Tree (AST)
   - Implements recursive descent parsing
   - Handles expressions, statements, and declarations
   - Supports object-oriented features (classes, methods, properties)
   - Includes error recovery through synchronization
   - Manages operator precedence and associativity

3. **Resolver** (`src/Zen/Execution/Resolver.cs`):
   - Performs variable resolution and scope analysis
   - Binds identifiers to their definitions
   - Validates variable usage and detects initialization errors
   - Handles lexical scoping for functions and blocks
   - Manages special scoping rules for classes and methods
   - Tracks function types (regular functions, methods, constructors)
   - Reports resolution errors with source locations

4. **Interpreter** (`src/Zen/Execution/Interpreter.cs`):
   The interpreter is split into multiple partial classes for better organization:
   - `Interpreter.Core.cs`: Core interpreter functionality and initialization
   - `Interpreter.ExprVisitor.cs`: Expression evaluation visitor implementation
   - `Interpreter.StmtVisitor.cs`: Statement execution visitor implementation
   - `Interpreter.FuncHandler.cs`: Function and method call handling
   - `Interpreter.Import.cs`: Executes import statements by delegating to the Importer class.

 ### Additional Systems

 There's also the Importer (`src/Zen/Execution/Import/Importer.cs`) which handles importing.
 
 The importer has Package, Namespace and Module as the main conceptual parts:

 - A Package is at minimum a directory containing 


### Concurrency Model

Zen implements a concurrency model similar to Node.js, using a single-threaded event loop with async/await support:

#### Event Loop
- Single-threaded execution model
- Tasks are queued and executed sequentially
- Non-blocking I/O operations are scheduled on the event loop
- Maintains a queue of pending tasks and tracks their completion

The runtime calls SynchronizationContext.SetSynchronizationContext(SyncContext);
The SyncContext is the ZenSynchronizationContext that handles the event loop.
This ensures all tasks run on the same thread.

#### Async Functions
- Declared using the `async` keyword
- Always return a Task
- When called, immediately schedule their execution on the event loop
- Continue executing the caller's code without blocking

#### Tasks
- Represent the eventual completion of an async operation
- Automatically created for async function returns

#### Await Expression
- Used inside async functions to wait for Task completion
- Suspends execution of the current function
- Resumes when the Task resolves or rejects
- Unwraps the Task result or throws on rejection

Example:
```zen
async func iFeelSleepy(ms: int): int {
    // Built-in function that returns a Task
    // which resolves after ms milliseconds
    return await delay(ms)
}

async func example() {
    var elapsed = await delay(100)  // Waits for 100ms
    print elapsed # ~100
}
```

Program Execution:
1. Top-level code runs synchronously
2. Async functions are scheduled on the event loop
3. Event loop processes tasks until all are complete
4. Program exits when no tasks remain


### Module System

Zen features a simple module system that supports packages, namespaces, and circular dependencies:

#### Structure
- **Packages**: A directory containing a package.zen file with `package [packagename]`.
- **Namespaces**: A folder in a package directory.
- **Modules**: Individual .zen files containing code

Modules can define one or more exportable symbols (I.E functions or classes).

#### Import System
The import system processes modules in phases to support circular dependencies:

1. **Parsing Phase**
   - Lexes and parses module source code
   - Creates AST representation
   - Identifies imports and dependencies

2. **Type Declaration Phase**
   - Creates placeholder types for classes and functions
   - Makes types available for reference before implementation
   - Enables circular dependencies to work

3. **Import Resolution Phase**
   - Resolves import statements
   - Links modules and their dependencies
   - Validates imports and symbols

4. **Execution Phase**
   - Executes module code in dependency order
   - Fills in actual implementations of types
   - Handles circular references through lazy initialization

#### Import Syntax
```zen
# Import entire module
import MyPackage.Utils

# Import specific symbols
from MyPackage.Utils import Logger, FileSystem

# Import with alias
import MyPackage.Utils as U
```

#### Module States
Modules progress through states during processing:
1. NotLoaded: Initial state
2. Parsing: During lexing/parsing
3. ParseComplete: AST is ready
4. DeclaringTypes: Creating placeholder types
5. ResolvingImports: Finding and validating imports
6. ImportsResolved: All imports found and validated
7. Executing: Running module code
8. Executed: Module fully executed

### Type System

Zen features a static type system with type inference. The type system includes:

#### Basic Types
- `int`: 32-bit integer
- `int64`: 64-bit integer
- `float`: 32-bit floating-point
- `float64`: 64-bit floating-point
- `bool`: Boolean value
- `string`: String value
- `void`: Represents no value
- `null`: Represents absence of value
- `Type`: Represents type values themselves
- `Task`: Represents a task that can be awaited

#### Nullable Types
Any type can be made nullable by appending a question mark (?). Nullable types can hold either a value of their base type or null.

```zen
// Non-nullable variables must have a value
var age: int = 25

// Nullable variables can be null
var name: string? = null
name = "John" // Valid: assigning string to string?

var requiredName: string = null // Error: can't assign null to non-nullable type
```

#### Type Checking
The `is` operator checks if a value matches a type:

```zen
var x = 5
print x is int     // true
print x is int?    // true (non-nullable can be assigned to nullable)
print x is string  // false

var y: int? = null
print y is int     // false (nullable might be null)
print y is int?    // true
```

#### Type Casting
Types can be cast using parentheses:

```zen
var x = 5.5
var y = (int) x    // Converts float to int
print y            // 5

var z: string? = "Hello"
var w = (string) z // Cast from string? to string (fails if z is null)
```

Type casting rules:
- Numeric types can be cast between each other
- Nullable types can be cast to their non-nullable version (fails if the value is null)
- Invalid casts throw a runtime error

### Builtin System

Zen provides an extensible builtin system through the `IBuiltinsProvider` interface. This allows for modular addition of built-in functionality:

- Core builtins are registered during interpreter initialization
- Each builtin provider implements `IBuiltinsProvider.RegisterBuiltins()`
- Current builtin modules include:
  - Core.Typing: Provides type-related operations
  - Core.Time: Provides time-related functions
- New builtin modules can be easily added by implementing IBuiltinsProvider

## TODO
- [x] Typing
- - [] Nullable types
- - [x] Type casting using parenthesis
- - [x] Type comparison using 'is'
- - [] Byte type, to make Interop with C# easier
- - [x] Union types, like type number = int|float
- [x] Classes
- - [] Static members (methods & properties)
- - [x] Interfaces
- - - [x] interface declaration statement
- - - [x] class support for interfaces via `implements` keyword
- - [] super() for calling parent constructor
- - - [] missing super() construcor call should cause Resolver to throw an error
- - [] super.method() for calling any parent method?
- [] Static analysis
- - [] Constant folding
- [x] Namespaces, packages & imports
- - [x] ImportStmt
- - [x] FromImportStmt
- - [] Make modules automatically import all sibling modules (modules of the same namespace) implicitly.
- - [] Ability to import namespaces and multi-symbol modules into a special Module Object
- - [x] Importer System
- - - [x] Two-phase import with placeholders for cyclic imports
- - - [x] MainScriptProvider that provides files from the current main script package
- - - [x] BuiltinProvider that can provide packages from embedded .zen resource files
- [x] Runtime
- - [x] Event loop implementation
- - [x] Async / Await
- - [] Error Handling
- - - [] Unified Error Handling and Exception interop with C#
- - - [] make the interpreter track the current token and make the interpreters Error static function use that as the SourceLocation for the error?
- - - [] Exception Throwing
- - - [] try/catch/finally statements
- - - [] stack traces
- [] Collections
- - [x] BracketGet and BracketSet interfaces
- - [x] Make builtin array class implement Bracket interfaces from stdlib packages? how?
- - [x] Array class and Bracket Access Interface using array[index]
- - [x] Iterable and Enumerator interfaces and integration with for-in loops
- - [] Map class for storing key-value pairs, with support for bracket access
- - [] Array literals
- - [] Map literals
- [] Dotnet Interop
- - [x] CallDotNet and CallDotNetAsync
- - [x] DotNetObject with automatic class generation (ZenProxyClass and ZenProxyObject)
- - [] Utility methods on DotNetObject for getting info about a DotNetObject, such as its class name.
- [] Standard library
- - [] Math
- - [] File
- - [] Standard IO
- - [] Date & Time
- - [] Low-level HTTP (tcp, sockets)
- - [] Windowing, GUI, Graphics

# License

MIT