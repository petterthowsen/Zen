# Zen Type System Analysis

## Current Architecture

### Core Components

1. **Type Representation**
   - `ZenType`: Represents types in the system (primitives, classes, generics)
   - `ZenTypeClass`: Represents class types specifically
   - `ZenValue`: Holds a type and its underlying value
   - `TypeHint`: AST node for type annotations in the source code

2. **Class System**
   - `ZenClass`: Defines class structure and behavior
   - `ZenObject`: Represents instances of classes
   - `Parameter`: Handles class parameters (both type and value parameters)

3. **Generic System**
   - Generic parameters marked during parsing
   - Type substitution during class instantiation
   - Support for default parameter values

## Current Workflow

1. **Parsing Phase**
   - TypeHints are parsed with generic parameter information
   - Generic names are tagged in type hints

2. **Class Definition**
   - Classes store parameters (type or value) with optional defaults
   - Creates a ZenTypeClass with parameter information

3. **Instantiation**
   - Parameters are validated
   - Generic types are substituted with concrete types
   - Properties and methods are concretized with actual types

## Issues and Potential Improvements

### 1. Type System Complexity

**Current Issues:**
- Multiple overlapping representations (ZenType, TypeHint, ZenTypeClass)
- Complex relationship between type representations
- Unclear boundaries between compile-time and runtime types

**Proposed Solutions:**
- Create a clearer separation between compile-time and runtime type representations
- Introduce a unified type representation system:
```typescript
interface Type {
    kind: 'primitive' | 'class' | 'generic' | 'parametric';
    name: string;
    parameters?: Type[];
    constraints?: TypeConstraint[];
}

interface TypeConstraint {
    kind: 'extends' | 'super' | 'equals';
    type: Type;
}
```

### 2. Generic Type Handling

**Current Issues:**
- Generic type substitution is complex and scattered
- Limited constraint system for generic parameters
- No support for generic methods independent of class generics

**Proposed Solutions:**
- Introduce a dedicated generic type resolver:
```typescript
class GenericResolver {
    resolveType(type: Type, typeArgs: Map<string, Type>): Type;
    validateConstraints(type: Type, constraints: TypeConstraint[]): boolean;
}
```
- Add support for generic method declarations:
```zen
func transform<T, U>(input: T, mapper: (T) -> U): U {
    return mapper(input)
}
```

### 3. Type Parameter Constraints

**Current Issues:**
- Limited support for type parameter constraints
- No way to specify relationships between type parameters

**Proposed Solutions:**
- Add support for advanced constraints:
```zen
class Container<T: Comparable> {
    // T must implement Comparable
}

class KeyValue<K, V: Dependent<K>> {
    // V depends on K
}
```

### 4. Type Inference

**Current Issues:**
- Type inference is basic
- No support for contextual type inference

**Proposed Solutions:**
- Implement bidirectional type checking
- Add support for type inference from usage:
```zen
class Box<T> {
    value: T
    map<U>(transform: (T) -> U): Box<U> {
        return Box(transform(value))
    }
}

// Should infer Box<int> -> Box<string>
var box = Box(42).map(x => x.toString())
```

## Implementation Plan

### Phase 1: Type System Refactoring
1. Create new type representation system
2. Implement type equality and compatibility checking
3. Add basic constraint system
4. Update parser to use new type system

### Phase 2: Generic System Enhancement
1. Implement GenericResolver
2. Add support for generic methods
3. Implement advanced constraint checking
4. Add generic type inference

### Phase 3: Type Inference Improvements
1. Implement bidirectional type checking
2. Add contextual type inference
3. Improve error messages
4. Add type inference for generic methods

### Phase 4: Testing and Documentation
1. Create comprehensive test suite
2. Document type system behavior
3. Add examples for common patterns
4. Create migration guide

## Code Structure

```
src/Zen/Typing/
├── Types/
│   ├── Type.cs             # Base type interface
│   ├── PrimitiveType.cs    # Primitive type implementation
│   ├── ClassType.cs        # Class type implementation
│   └── GenericType.cs      # Generic type implementation
├── Resolution/
│   ├── TypeResolver.cs     # Type resolution logic
│   ├── GenericResolver.cs  # Generic type resolution
│   └── Constraints.cs      # Constraint checking
└── Inference/
    ├── TypeInference.cs    # Type inference engine
    └── Context.cs          # Type context tracking
```

## Benefits

1. **Clearer Architecture**
   - Separation of concerns between different type system components
   - More maintainable and testable code
   - Easier to extend with new features

2. **Better Generic Support**
   - More powerful generic constraints
   - Support for generic methods
   - Improved type inference

3. **Enhanced Developer Experience**
   - Better error messages
   - More predictable type inference
   - Clearer documentation

4. **Future Extensibility**
   - Easier to add new type system features
   - Better support for tooling
   - Foundation for IDE support
