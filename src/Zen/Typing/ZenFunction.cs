using Environment = Zen.Execution.Environment;
using Interpreter = Zen.Execution.Interpreter;

namespace Zen.Typing;

public abstract class ZenFunction : ICallable {

    public struct Parameter {
        public readonly string Name { get; init; }
        public readonly ZenType Type { get; init; }
        public readonly bool Nullable { get; init; }
        public readonly ZenValue? DefaultValue { get; init; }

        public Parameter(string name, ZenType type) {
            Name = name;
            Type = type;
            Nullable = true;
            DefaultValue = null;
        }

        public Parameter(string name, ZenType type, bool nullable) : this(name, type) {
            Nullable = nullable;
            DefaultValue = ZenValue.Null;
        }

        public Parameter(String name, ZenType type, bool nullable, ZenValue defaultValue) : this(name, type) {
            Nullable = nullable;
            DefaultValue = defaultValue;
        }
    }

    public int Arity => Parameters.Length;
    
    private readonly ZenType _returnType;
    public ZenType ReturnType => _returnType;
    public readonly Parameter[] Parameters;

    public Environment? Closure = null;

    public ZenFunction(ZenType returnType, Parameter[] parameters) {
        _returnType = returnType;
        Parameters = parameters;
    }

    public ZenFunction(ZenType returnType, Parameter[] parameters, Environment closure) : this(returnType, parameters) {
        Closure = closure;
    }
    
    public abstract ZenValue Call(Interpreter interpreter, ZenValue[] arguments);

    public override string ToString() {
        return $"Function";
    }
}