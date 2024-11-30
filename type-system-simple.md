# Simplified Type System Improvements

## Current System

```csharp
// In ZenClass
public struct Parameter {
    public string Name;      // e.g., "T" or "MAX"
    public ZenType Type;     // ZenType.Type for T, ZenType.Integer for MAX:int
    public ZenValue DefaultValue;
}

// In ZenType
public class ZenType {
    public string Name;
    public bool IsNullable;
    public ZenType[] Parameters;  // For generic type arguments
}
```

## Proposed Improvements

### 1. Clearer Generic Parameter Handling

```csharp
// In ZenClass.Parameter
public bool IsTypeParameter => Type == ZenType.Type;
public bool IsValueParameter => !IsTypeParameter;

// Helper methods
public bool ValidateValue(ZenValue value) {
    if (IsTypeParameter) {
        return value.Type == ZenType.Type;
    }
    return Type.IsAssignableFrom(value.Type);
}
```

### 2. Simplified Type Substitution

```csharp
// In ZenClass
private Dictionary<string, ZenType> ResolveTypeParameters(Dictionary<string, ZenValue> paramValues) {
    var substitutions = new Dictionary<string, ZenType>();
    
    foreach (var param in Parameters) {
        if (!param.IsTypeParameter) continue;
        
        if (paramValues.TryGetValue(param.Name, out var value)) {
            // For type parameters, the value should be a ZenType
            substitutions[param.Name] = (ZenType)value.Underlying!;
        }
    }
    
    return substitutions;
}

// Use during method/property type resolution
public ZenType SubstituteType(ZenType original, Dictionary<string, ZenType> substitutions) {
    // If this is a generic parameter name (e.g., "T"), substitute it
    if (original.IsGeneric && substitutions.TryGetValue(original.Name, out var concrete)) {
        return concrete;
    }
    
    // If it's a parametric type, substitute in its parameters
    if (original.IsParametric) {
        var newParams = original.Parameters.Select(p => SubstituteType(p, substitutions)).ToArray();
        return new ZenType(original.Name, original.IsNullable, newParams);
    }
    
    return original;
}
```

### 3. Improved Type Checking

```csharp
// In TypeChecker
public static bool IsCompatible(ZenType source, ZenType target) {
    // Handle basic cases
    if (target == ZenType.Any) return true;
    if (source == target) return true;
    if (target.IsNullable && !source.IsNullable && source.Name == target.Name) return true;
    
    // Handle parametric types (like Container<string>)
    if (source.IsParametric && target.IsParametric) {
        if (source.Name != target.Name) return false;
        if (source.Parameters.Length != target.Parameters.Length) return false;
        
        for (int i = 0; i < source.Parameters.Length; i++) {
            if (!IsCompatible(source.Parameters[i], target.Parameters[i])) {
                return false;
            }
        }
        return true;
    }
    
    return false;
}
```

## Implementation Steps

1. Add helper methods to `ZenClass.Parameter` to clarify parameter kinds
2. Implement type substitution in `ZenClass`
3. Update type checking to handle generic parameters properly
4. Add tests for:
   - Generic class instantiation
   - Generic method calls
   - Type parameter validation

## Example Usage

```zen
// Define generic class
class Container<T> {
    value: T
    Container(v: T) {
        this.value = v
    }
    
    get(): T {
        return this.value
    }
}

// Usage
var strBox = new Container<string>("hello")
var intBox = new Container<int>(42)

print strBox.get()  // "hello"
print intBox.get()  // 42
