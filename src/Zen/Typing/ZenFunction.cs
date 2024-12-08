using Environment = Zen.Execution.Environment;
using Zen.Parsing.AST.Expressions;
using Zen.Parsing.AST;
using Zen.Common;

namespace Zen.Typing;

public class ZenFunction : ICallable {

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

    public enum TYPE {
        UserFunction,
        HostFunction,
        HostMethod,
        UserMethod
    }

    public bool Async { get; set; } = false;
    public bool Variadic { get; set; } = false;
    public ZenType ReturnType { get; set; } = ZenType.Void;
    public List<Argument> Arguments { get; set; } = [];
    public Environment? Closure { get; set; }
    public ZenClass.Visibility Visibility { get; set; } = ZenClass.Visibility.Public;
    public TYPE Type { get; set; }

    public int Arity => Arguments.Count;
    public bool IsUser => Type == TYPE.UserFunction || Type == TYPE.UserMethod;
    public bool IsHost => Type == TYPE.HostFunction || Type == TYPE.HostMethod;

    public bool IsMethod => Type == TYPE.HostMethod || Type == TYPE.UserMethod;

    // User-related metadata
    public TypeHint? ReturnTypeHint { get; set; }  // Store original return type hint for resolving generics
    
    // Function code
    public Block? UserCode { get; set; }
    public Func<ZenValue[], ZenValue>? Func { get; set; }
    public Func<ZenValue[], Task<ZenValue>>? AsyncHostFunc { get; set; }
    public Func<ZenObject, ZenValue[], ZenValue>? HostMethod { get; set; }
    public Func<ZenObject, ZenValue[], Task<ZenValue>>? AsyncHostMethod { get; set; }
    public Func<ZenValue[], ZenValue>? StaticHostMethod { get; set; }

    // Method-related metadata
    public bool IsStatic { get; set; } = false;
    public string? Name { get; set; }

    public ZenFunction(TYPE type, bool async, bool variadic, ZenType returnType, List<Argument> arguments) {
        Type = type;
        Async = async;
        Variadic = variadic;
        ReturnType = returnType;
        Arguments = arguments;
    }

    // Create a sync host function
    public static ZenFunction NewHostFunction(ZenType returnType, List<Argument> arguments, Func<ZenValue[], ZenValue> func, bool variadic = false) 
    {
        return new ZenFunction(TYPE.HostFunction, false, variadic, returnType, arguments) {
            Func = func
        };
    }

    // Create a Async host function
    public static ZenFunction NewAsyncHostFunction(ZenType returnType, List<Argument> arguments, Func<ZenValue[], Task<ZenValue>> asyncFunc, bool variadic = false) 
    {
        return new ZenFunction(TYPE.HostFunction, true, variadic, returnType, arguments) {
            AsyncHostFunc = asyncFunc
        };
    }

    // Create a host method
    public static ZenFunction NewHostMethod(string name, ZenType returnType, List<Argument> arguments, Func<ZenObject, ZenValue[], ZenValue> method, bool variadic = false) {
        return new ZenFunction(TYPE.HostMethod, false, variadic, returnType, arguments) {
            Name = name,
            HostMethod = method,
        };
    }

    // Create a static host method
    public static ZenFunction NewStaticHostMethod(string name, ZenType reurnType, List<Argument> arguments, Func<ZenValue[], ZenValue> method, bool variadic = false) {
        return new ZenFunction(TYPE.HostMethod, false, variadic, reurnType, arguments) {
            Name = name,
            StaticHostMethod = method,
            IsStatic = true
        };
    }

    // Create a Async host method
    public static ZenFunction NewAsyncHostMethod(string name, ZenType returnType, List<Argument> arguments, Func<ZenObject, ZenValue[], Task<ZenValue>> method, bool variadic = false) {
        return new ZenFunction(TYPE.HostMethod, true, variadic, returnType, arguments) {
            Name = name,
            AsyncHostMethod = method,
        };
    }

    // Create a user function
    public static ZenFunction NewUserFunction(ZenType returnType, List<Argument> arguments, Block? block, Environment? closure, bool async = false, bool variadic = false)
    {
        return new ZenFunction(TYPE.UserFunction, async, false, returnType, arguments) {
            UserCode = block,
            Closure = closure,
        };
    }

    // create a user method
    public static ZenFunction NewUserMethod(string name, ZenType returnType, List<Argument> arguments, Block? block, Environment? closure, bool async, bool variadic = false) {
        return new ZenFunction(TYPE.UserMethod, async, variadic, returnType, arguments) {
            Name = name,
            UserCode = block,
            Closure = closure,
        };
    }

    /// <summary>
    /// Create a shallow copy
    /// </summary>
    /// <returns></returns>
    public ZenFunction Clone()
    {
        return (ZenFunction)MemberwiseClone();
    }

    public ZenFunction Clone(ZenType returnType, List<Argument> arguments) {
        var clone = Clone();
        clone.ReturnType = returnType;
        clone.Arguments = arguments;
        return clone;
    }

    /// <summary>
    /// Bind this function to an instance. The returned function is a shallow copy with the BoundInstance field set.
    /// </summary>
    public virtual BoundMethod Bind(ZenObject instance) {
        if (false == IsMethod) {
            throw new Exception("Can only bind methods to a ZenObject instance!");
        }

        if (IsStatic) {
            throw new Exception("Cannot bind static methods to a ZenObject instance!");
        }

        return new BoundMethod(instance, this);

    }

    public override string ToString() {
        string argStr = string.Join(", ", Arguments.Select(a => a.ToString()));
        string asyncStr = Async ? " (Async)" : "";
        
        string prefix = "Function";
        switch (Type) {
            case TYPE.HostFunction:
                prefix = "HostFunction";
                break;
            case TYPE.UserFunction:
                prefix = "UserFunction";
                break;
            case TYPE.HostMethod:
                prefix = "HostMethod";
                break;
            case TYPE.UserMethod:
                prefix = "UserMethod";
                break;
        }
        
        return $"{prefix}{asyncStr} {Name}({argStr}): {ReturnType}";
    }
}
