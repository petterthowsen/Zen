# Zen Language Type System Improvements Proposal

## Current Implementation Analysis

The current implementation has several key components:
- `ZenType`: Represents types including parametric types (struct)
- `ZenValue`: Represents primitive values (readonly struct)
- `Variable`: Represents variables that can hold values or object references (class)
- Interpreter using visitor pattern with `dynamic?` returns

### Current Challenges

1. Use of `dynamic?` in visitor pattern makes type checking difficult and error-prone
2. Type conversion/promotion logic is scattered across different methods
3. Parametric type checking is not fully implemented
4. Type compatibility rules are not clearly defined in one place

## Proposed Improvements

### 1. Visitor Pattern Return Types

Replace `dynamic?` with a proper type hierarchy:

```csharp
public interface IEvaluationResult {
    ZenType Type { get; }
    bool IsTruthy();
}

public readonly struct ValueResult : IEvaluationResult {
    public ZenValue Value { get; }
    public ZenType Type => Value.Type;
    public bool IsTruthy() => Value.IsTruthy();
}

public class VariableResult : IEvaluationResult {
    public Variable Variable { get; }
    public ZenType Type => Variable.Type;
    public bool IsTruthy() => Variable.IsTruthy();
}

public class VoidResult : IEvaluationResult {
    public static readonly VoidResult Instance = new();
    public ZenType Type => ZenType.Void;
    public bool IsTruthy() => false;
}
```

Update the visitor interface:
```csharp
public interface IVisitor<T> where T : IEvaluationResult {
    T Visit(Binary binary);
    T Visit(Unary unary);
    // etc...
}
```

### 2. Type System Improvements

#### 2.1 Type Compatibility Checking

Create a dedicated TypeChecker class:

```csharp
public class TypeChecker {
    public static bool IsCompatible(ZenType source, ZenType target) {
        // Base cases
        if (target == ZenType.Any) return true;
        if (source == target) return true;
        
        // Handle numeric promotions
        if (source.IsNumeric && target.IsNumeric) {
            return IsValidNumericPromotion(source, target);
        }

        // Handle parametric types
        if (source.IsParametric && target.IsParametric) {
            return CheckParametricTypeCompatibility(source, target);
        }

        return false;
    }

    private static bool IsValidNumericPromotion(ZenType source, ZenType target) {
        // Allow widening conversions only
        if (source == ZenType.Integer) {
            return target == ZenType.Integer64 || target == ZenType.Float || target == ZenType.Float64;
        }
        if (source == ZenType.Float) {
            return target == ZenType.Float64;
        }
        if (source == ZenType.Integer64) {
            return target == ZenType.Float64;
        }
        return false;
    }

    private static bool CheckParametricTypeCompatibility(ZenType source, ZenType target) {
        if (source.Name != target.Name) return false;
        if (source.Parameters.Length != target.Parameters.Length) return false;
        
        for (int i = 0; i < source.Parameters.Length; i++) {
            if (!IsCompatible(source.Parameters[i], target.Parameters[i])) {
                return false;
            }
        }
        
        return true;
    }
}
```

#### 2.2 Type Conversion System

```csharp
public static class TypeConverter {
    public static ZenValue Convert(ZenValue value, ZenType targetType) {
        if (value.Type == targetType) return value;
        if (!TypeChecker.IsCompatible(value.Type, targetType)) {
            throw new TypeError($"Cannot convert from {value.Type} to {targetType}");
        }

        return targetType switch {
            var t when t == ZenType.Integer64 => ConvertToInt64(value),
            var t when t == ZenType.Float => ConvertToFloat(value),
            var t when t == ZenType.Float64 => ConvertToFloat64(value),
            var t when t == ZenType.String => ConvertToString(value),
            _ => throw new TypeError($"Unsupported conversion from {value.Type} to {targetType}")
        };
    }

    private static ZenValue ConvertToInt64(ZenValue value) {
        if (value.Type == ZenType.Integer) {
            return new ZenValue(ZenType.Integer64, (long)(int)value.Underlying);
        }
        throw new TypeError($"Cannot convert {value.Type} to Int64");
    }

    // Additional conversion methods...
}
```

### 3. Binary Operations Type Rules

Define clear rules for type promotion in binary operations:

```csharp
public class BinaryOperationRules {
    public static ZenType DetermineResultType(TokenType op, ZenType left, ZenType right) {
        return op switch {
            TokenType.Plus when left.IsNumeric && right.IsNumeric 
                => GetNumericPromotionType(left, right),
            TokenType.Plus when left == ZenType.String || right == ZenType.String 
                => ZenType.String,
            TokenType.Minus or TokenType.Star or TokenType.Slash when left.IsNumeric && right.IsNumeric 
                => GetNumericPromotionType(left, right),
            _ => throw new TypeError($"Invalid operation {op} between types {left} and {right}")
        };
    }

    private static ZenType GetNumericPromotionType(ZenType a, ZenType b) {
        if (a == ZenType.Float64 || b == ZenType.Float64) return ZenType.Float64;
        if (a == ZenType.Float || b == ZenType.Float) return ZenType.Float;
        if (a == ZenType.Integer64 || b == ZenType.Integer64) return ZenType.Integer64;
        return ZenType.Integer;
    }
}
```

### 4. Implementation Strategy

1. First implement the `IEvaluationResult` hierarchy and update the visitor interface
2. Add the TypeChecker and TypeConverter classes
3. Update the Interpreter to use the new type system:
   - Replace dynamic returns with IEvaluationResult
   - Use TypeChecker for compatibility checks
   - Use TypeConverter for conversions
   - Use BinaryOperationRules for operation type checking

### 5. Benefits

1. **Type Safety**: Eliminating `dynamic?` reduces runtime errors
2. **Maintainability**: Clear separation of type checking and conversion logic
3. **Extensibility**: Easy to add new types and type conversion rules
4. **Performance**: Reduced runtime type checking overhead
5. **Clarity**: Clear rules for type promotion and compatibility

### 6. Example Usage

```csharp
public class Interpreter : IVisitor<IEvaluationResult> {
    public IEvaluationResult Visit(Binary binary) {
        var left = Evaluate(binary.Left);
        var right = Evaluate(binary.Right);

        // Get actual values
        var leftValue = left is VariableResult varLeft ? varLeft.Variable.GetZenValue() : ((ValueResult)left).Value;
        var rightValue = right is VariableResult varRight ? varRight.Variable.GetZenValue() : ((ValueResult)right).Value;

        // Check operation validity and get result type
        var resultType = BinaryOperationRules.DetermineResultType(binary.Operator.Type, leftValue.Type, rightValue.Type);

        // Perform operation
        var result = PerformBinaryOperation(binary.Operator.Type, leftValue, rightValue);
        
        return new ValueResult(result);
    }
}
