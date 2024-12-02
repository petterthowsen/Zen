using Environment = Zen.Execution.Environment;
using Interpreter = Zen.Execution.Interpreter;
using Zen.Parsing.AST.Expressions;

namespace Zen.Typing;

public abstract class ZenFunction : ICallable {

    public struct Argument {
        public readonly string Name { get; init; }
        public readonly ZenType Type { get; init; }
        public readonly bool Nullable { get; init; }
        public readonly ZenValue? DefaultValue { get; init; }
        public readonly TypeHint? OriginalTypeHint { get; init; }  // Store original type hint for resolving generics

        public Argument(string name, ZenType type) {
            Name = name;
            Type = type;
            Nullable = true;
            DefaultValue = null;
            OriginalTypeHint = null;
        }

        public Argument(string name, ZenType type, bool nullable) : this(name, type) {
            Nullable = nullable;
            DefaultValue = ZenValue.Null;
        }

        public Argument(string name, ZenType type, bool nullable, ZenValue defaultValue) : this(name, type) {
            Nullable = nullable;
            DefaultValue = defaultValue;
        }

        public Argument(string name, ZenType type, bool nullable, ZenValue? defaultValue, TypeHint? originalTypeHint) {
            Name = name;
            Type = type;
            Nullable = nullable;
            DefaultValue = defaultValue;
            OriginalTypeHint = originalTypeHint;
        }

        public override readonly string ToString()
        {
            return $"{Name}: {Type}{(Nullable ? "?" : "")}{(DefaultValue != null ? $" = {DefaultValue}" : "")}";
        }
    }

    public int Arity => Arguments.Count;
    
    public bool Async = false;

    public ZenType ReturnType { get; set; }
    public TypeHint? ReturnTypeHint { get; set; }  // Store original return type hint for resolving generics
    public List<Argument> Arguments;

    public Environment? Closure = null;

    public ZenFunction(bool async, ZenType returnType, List<Argument> arguments) {
        ReturnType = returnType;
        Arguments = arguments;
        Async = async;
    }

    public ZenFunction(bool async, ZenType returnType, List<Argument> parameters, Environment closure) : this(async, returnType, parameters) {
        Async = async;
        Closure = closure;
    }

    public ZenFunction(bool async, ZenType returnType, TypeHint? returnTypeHint, List<Argument> arguments, Environment? closure = null) {
        ReturnType = returnType;
        ReturnTypeHint = returnTypeHint;
        Arguments = arguments;
        Async = async;
        Closure = closure;
    }
    
    public abstract ZenValue Call(Interpreter interpreter, ZenValue[] argumentValues);

    public override string ToString() {
        return $"Function";
    }
}
