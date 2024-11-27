# Zen

Zen is a high level programming language implemented in C#. The interpreter is currently a tree walk interpreter and follows the Crafting Interpreters book.

# About Zen

Zen is a high-level programming language inspired by modern languages such as C#, Java, Go, Python, Ruby, PHP, Node JS and others.

At a glance, Zen
- Has simple syntax.
- Strict typing with type inference.
- Functions and closures as first-class citizens.
- Runs in a single-threaded event loop similar to Node JS and supports async/await as syntactic sugar over promises.
- Fully cross platform across Windows, Mac and Linux.
- C and C# Interop

## Installing

Binaries for windows, mac & linux will be made available when Zen is more mature or upon request. It's pretty easy to build it yourself - all you need is dotnet with SDK 8.0 or later.

## Building and Running

To build and run the project:

```bash
# Build the project
dotnet build

# Run the project
dotnet run --project src/Zen

# to build and run, a helper script 'zen.sh' exists which will drop you in a Zen REPL.
./zen.sh
```

## Testing

To run the tests:

```bash
# Run all tests
dotnet test

# Run tests with coverage (optional)
dotnet test --collect:"XPlat Code Coverage"
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
   - `Interpreter.Import.cs`: Module import and symbol resolution

   Key features:
   - Executes the AST using a tree-walk interpreter
   - Implements a scope-based environment system
   - Provides runtime type checking and type conversion
   - Supports function calls with parameter validation
   - Handles object instantiation and method calls
   - Manages global and local variable resolution
   - Includes built-in functions through the IBuiltinsProvider interface

The system uses a visitor pattern for traversing and executing the AST, with separate visitors for different phases of execution. The architecture supports features like:
- Static typing with type inference
- Object-oriented programming with classes and inheritance
- First-class functions and closures
- Scoped variable resolution
- Runtime error handling with source locations
- Module system with imports and namespaces

### Concurrency Model

Zen implements a concurrency model similar to Node.js, using a single-threaded event loop with async/await support:

#### Event Loop
- Single-threaded execution model
- Tasks are queued and executed sequentially
- Non-blocking I/O operations are scheduled on the event loop
- Maintains a queue of pending tasks and tracks their completion

#### Async Functions
- Declared using the `async` keyword
- Always return a Promise
- When called, immediately schedule their execution on the event loop
- Continue executing the caller's code without blocking

#### Promises
- Represent the eventual completion of an async operation
- Can be in one of three states: pending, resolved, or rejected
- Support Then/Catch callbacks for handling completion
- Automatically created for async function returns

#### Await Expression
- Used inside async functions to wait for Promise completion
- Suspends execution of the current function
- Resumes when the Promise resolves or rejects
- Unwraps the Promise result or throws on rejection

Example:
```zen
async func delay(ms: int): int {
    // Built-in function that returns a Promise
    // which resolves after ms milliseconds
    return await delay(ms)
}

async func example() {
    var start = time()
    var result = await delay(100)  // Waits for 100ms
    var elapsed = time() - start
    print elapsed >= 100  // true
}

// Call async function without await
example()  // Returns immediately
print "This runs first"
```

Program Execution:
1. Top-level code runs synchronously
2. Async functions are scheduled on the event loop
3. Event loop processes tasks until all are complete
4. Program exits when no tasks remain

This model enables:
- Non-blocking I/O operations
- Concurrent execution of async tasks
- Predictable single-threaded execution
- Easy handling of asynchronous operations

### Module System

Zen features a robust module system that supports packages, namespaces, and circular dependencies:

#### Structure
- **Packages**: Root namespaces defined in package.zen files
- **Namespaces**: Organizational units that can contain modules and sub-namespaces
- **Modules**: Individual .zen files containing code

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
// Import entire module
import MyPackage.Utils

// Import specific symbols
from MyPackage.Utils import Logger, FileSystem

// Import with alias
import MyPackage.Utils as U
```

#### Circular Dependencies
The system fully supports circular dependencies through:
- Early type declaration
- Lazy initialization
- Forward references

Example:
```zen
// A.zen
from MyPackage.B import B

class A {
    var b: B
}

// B.zen
from MyPackage.A import A

class B {
    var a: A
}
```

This works because:
1. Types A and B are declared as placeholders
2. Type hints can reference the placeholders
3. Actual implementations are filled in during execution

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
- `type`: Represents type values themselves

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
- [] Improved error handling
- - [] make the interpreter track the current token and make the interpreters Error static function use that as the SourceLocation for the error.

- [x] Typing
- - [x] Nullable types
- - [x] Type casting using parenthesis
- - [x] Type comparison using 'is'
- - [] Union types, like type number = int|float
- [x] Classes
- - [] Interfaces
- - - [] interface declaration statement
- - - [] class support for interfaces via `implements` keyword
- - [] super() for calling parent constructor
- - - [] missing super() construcor call should cause Resolver to throw an error
- - [] super.method() for calling any parent method?
- [] Static analysis
- - [] Constant folding
- [x] Namespaces, packages & imports
- - [x] ImportStmt
- - [x] FromImportStmt
- - [x] Importer System
- - - [x] Two-phase import with placeholders for cyclic imports
- - - [x] MainScriptProvider that provides files from the current main script package
- - - [x] BuiltinProvider that can provide packages from embedded .zen resource files
- [x] Runtime
- - [x] Event loop implementation
- - [x] Async / Await
- - [] Exceptions
- - - [] Exception Throwing
- - - [] try/catch/finally statements
- [] Collections
- - [] Array class and Bracket Access using array[index]
- - [] Map class for storing key-value pairs, with support for bracket access
