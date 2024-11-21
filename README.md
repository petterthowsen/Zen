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


Note: Each project directory (src/Zen and tests/Zen.Tests) contains its own `bin` and `obj` directories for build outputs. These are automatically generated by .NET and are excluded from source control via .gitignore.

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

### Builtin System

Zen provides an extensible builtin system through the `IBuiltinsProvider` interface. This allows for modular addition of built-in functionality:

- Core builtins are registered during interpreter initialization
- Each builtin provider implements `IBuiltinsProvider.RegisterBuiltins()`
- Current builtin modules include:
  - Core.Typing: Provides type-related operations
  - Core.Time: Provides time-related functions
- New builtin modules can be easily added by implementing IBuiltinsProvider

## TODO

- [] Typing
- - [] Nullable types
- - [] Type casting using parenthesis
- - [] Type comparison using 'is'
- - [] Union types, like type number = int|float
- [] Classes
- - [] Interfaces
- - - [] interface declaration statement
- - - [] class support for interfaces via `implements` keyword
- - [] super() for calling parent constructor
- - - [] missing super() construcor call should cause Resolver to throw an error
- - [] super.method() for calling any parent method?
- [] Static analysis
- - [] Constant folding
- [] Namespaces, packages & imports
- - [] Ability to define nested symbols in the environment
- - [] System for keeping track of the current executed file and the "working directory"
- - [] `ìmport` system to import top-level public symbols from an files, with support for aliases
- - [] `include` builtin function for executing the given file relative to the current file OR working directory? Not sure
- - [] set up some Interpreter constants for the ZEN_HOME where packages are located
- [] Runtime
- - [] Event loop implementation
- - [] Async / Await
- - [] Exceptions
- - - [] Exception Throwing
- - - [] try/catch/finally statements
- [] Collections
- - [] Array class and Bracket Access using array[index]
- - [] Map class for storing key-value pairs, with support for bracket access