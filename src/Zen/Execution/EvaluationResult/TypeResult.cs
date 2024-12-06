using Zen.Typing;

namespace Zen.Execution.EvaluationResult;

/// <summary>
/// Represents the result of evaluating a TypeHint expression.
/// </summary>
public readonly struct TypeResult : IEvaluationResult {

    // the actual type
    public ZenType Type { get; init; } // the type

    // 
    public ZenValue Value { get; } = ZenValue.Void; // this is always a ZenType
    
    public bool IsTruthy() => Value.IsTruthy();
    public bool IsCallable() => Value.IsCallable();

    public bool IsClass() => Type.IsClass;

    public TypeResult(ZenType type, bool nullable = false)
    {
        Type = type;
        // if (nullable && Type.IsNullable == false) {
        //     Type = Type.MakeNullable();
        // }
        Value = new ZenValue(ZenType.Type, Type);
    }

    public TypeResult(ZenValue value, bool nullable = false)
    {
        if (value.Type == ZenType.Type) {
            Type = value.Underlying!;
        }else {
            Type = value.Type;
        }

        // if (nullable && Type.IsNullable == false) {
        //     Type = Type.MakeNullable();
        // }

        Value = new ZenValue(ZenType.Type, Type);
    }

    public static implicit operator TypeResult(ZenType type) => new TypeResult(type);
}