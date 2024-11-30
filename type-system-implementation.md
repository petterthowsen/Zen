# Zen Type System Implementation Plan

## Current State Analysis

The current type system has several key components:
1. `ZenType`: Base type representation with support for parametric types
2. `ZenClass`: Class definition with generic parameter support
3. `TypeChecker`: Basic type compatibility checking
4. `TypeHint`: AST representation of types in source code

## Phase 1: Core Type System Cleanup

### 1. Unified Type Representation
```csharp
public abstract class ZenType {
    public string Name { get; }
    public bool IsNullable { get; }
    public ZenType[] Parameters { get; }
    
    // New: Track constraints on type parameters
    public TypeConstraint[] Constraints { get; }
}

public class TypeConstraint {
    public enum Kind { Extends, Super, Equals }
    public Kind ConstraintKind { get; }
    public ZenType Type { get; }
}

// Specific type implementations
public class PrimitiveType : ZenType { }
public class ClassType : ZenType {
    public ZenClass Declaration { get; }
}
public class GenericParameterType : ZenType {
    public bool IsTypeParameter { get; } // true for T in class List<T>
    public bool IsValueParameter { get; } // true for N in class Array<N: int>
}
```

### 2. Enhanced Type Parameter System
```csharp
public class TypeParameter {
    public string Name { get; }
    public ZenType Type { get; }  // Type of the parameter (e.g., Type for T, int for N)
    public ZenValue? DefaultValue { get; }
    public TypeConstraint[] Constraints { get; }
}

// Usage in ZenClass
public class ZenClass {
    public TypeParameter[] TypeParameters { get; }
    
    // New: Track substituted types for generic instances
    private Dictionary<string, ZenType> _typeSubstitutions;
    
    public ZenType ResolveType(ZenType type) {
        // Resolve type using current substitutions
        if (type is GenericParameterType gpt && _typeSubstitutions.TryGetValue(gpt.Name, out var concrete)) {
            return concrete;
        }
        return type;
    }
}
```

### 3. Improved Type Checking
```csharp
public class TypeChecker {
    public bool IsCompatible(ZenType source, ZenType target, TypeContext context) {
        // Enhanced type compatibility checking with context
        if (target is GenericParameterType gpt) {
            return ValidateConstraints(source, gpt.Constraints, context);
        }
        
        // Existing compatibility rules...
        return false;
    }
    
    private bool ValidateConstraints(ZenType type, TypeConstraint[] constraints, TypeContext context) {
        foreach (var constraint in constraints) {
            switch (constraint.ConstraintKind) {
                case TypeConstraint.Kind.Extends:
                    if (!IsCompatible(type, constraint.Type, context))
                        return false;
                    break;
                // Handle other constraint kinds...
            }
        }
        return true;
    }
}
```

## Phase 2: Generic Method Support

### 1. Method Type Parameters
```csharp
public class ZenMethod {
    public TypeParameter[] TypeParameters { get; }
    
    // New: Type inference for method type parameters
    public bool TryInferTypeArguments(
        ZenType[] argumentTypes,
        out Dictionary<string, ZenType> typeArguments
    );
}
```

### 2. Generic Method Resolution
```csharp
public class MethodResolver {
    public ZenMethod ResolveMethod(
        string name,
        ZenType[] argumentTypes,
        Dictionary<string, ZenType>? explicitTypeArgs = null
    ) {
        foreach (var method in methods) {
            if (method.Name != name) continue;
            
            // Try explicit type arguments first
            if (explicitTypeArgs != null) {
                if (IsValidMethodCall(method, argumentTypes, explicitTypeArgs))
                    return method;
                continue;
            }
            
            // Try type inference
            if (method.TryInferTypeArguments(argumentTypes, out var inferredTypes))
                if (IsValidMethodCall(method, argumentTypes, inferredTypes))
                    return method;
        }
        throw new Error($"No matching method '{name}' found");
    }
}
```

## Phase 3: Type Inference Improvements

### 1. Bidirectional Type Checking
```csharp
public class TypeInference {
    // Synthesize type from expression
    public ZenType Synthesize(Expr expr, TypeContext context);
    
    // Check expression against expected type
    public bool Check(Expr expr, ZenType expected, TypeContext context);
    
    // Infer type arguments from context
    public bool InferTypeArguments(
        TypeParameter[] parameters,
        ZenType[] argumentTypes,
        ZenType expectedType,
        out Dictionary<string, ZenType> typeArguments
    );
}
```

### 2. Context Tracking
```csharp
public class TypeContext {
    private Dictionary<string, ZenType> _typeVariables;
    private ZenType? _expectedType;
    
    public void PushExpectedType(ZenType type);
    public void PopExpectedType();
    
    public void AddTypeVariable(string name, ZenType type);
    public bool TryGetTypeVariable(string name, out ZenType type);
}
```

## Implementation Steps

1. **Core Updates (Week 1)**
   - Implement new ZenType hierarchy
   - Add TypeConstraint system
   - Update TypeChecker with constraint validation

2. **Generic Methods (Week 2)**
   - Add TypeParameter support to ZenMethod
   - Implement method type inference
   - Update method resolution

3. **Type Inference (Week 3)**
   - Implement bidirectional type checking
   - Add context tracking
   - Update expression visitor

4. **Testing (Week 4)**
   - Add tests for constraints
   - Add tests for generic methods
   - Add tests for type inference
   - Add tests for error cases

## Example Usage

```zen
// Generic class with constraint
class Container<T: Comparable> {
    value: T
    
    // Generic method with additional type parameter
    map<U>(transform: (T) -> U): Container<U> {
        return Container(transform(this.value))
    }
    
    // Method using type constraint
    compareTo(other: Container<T>): int {
        return this.value.compareTo(other.value)
    }
}

// Type inference example
var container = Container(42)              // Infers Container<int>
var mapped = container.map(x => x * 2)     // Infers Container<int>
var str = container.map(x => x.toString()) // Infers Container<string>
