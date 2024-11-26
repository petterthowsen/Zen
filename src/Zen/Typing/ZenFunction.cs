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

    public int Arity => Parameters.Count;
    
    public bool Async = false;

    private readonly ZenType _returnType;
    public ZenType ReturnType => _returnType;
    public List<Parameter> Parameters;

    public Environment? Closure = null;

    public ZenFunction(bool async, ZenType returnType, List<Parameter> parameters) {
        _returnType = returnType;
        Parameters = parameters;
        Async = async;
    }

    public ZenFunction(bool async, ZenType returnType, List<Parameter> parameters, Environment closure) : this(async, returnType, parameters) {
        Async = async;
        Closure = closure;
    }
    
    public abstract ZenValue Call(Interpreter interpreter, ZenValue[] arguments);

    public override string ToString() {
        return $"Function";
    }
}