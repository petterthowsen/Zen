using System.Diagnostics;
using Zen.Common;
using Zen.Execution;
using Zen.Parsing.AST.Expressions;

namespace Zen.Typing;

public class ZenClass {

    public static readonly ZenClass Master = new ZenClass("Master", [
        // methods
        new ZenHostMethod(false, "ToString", Visibility.Public, ZenType.String, [], (ZenObject instance, ZenValue[] args) => {
            return new ZenValue(ZenType.String, "Object(" + instance.Class.Name + ")");
        }),
    ], [
        // properties
    ], []);

    public enum Visibility {
        Public,
        Private,
        Protected
    }

    public struct Property {
        public string Name;
        public ZenType Type;
        public ZenValue Default;
        public Visibility Visibility;

        public bool IsGeneric => Type.IsGeneric;

        public Property(string name, ZenType type, ZenValue defaultValue, Visibility visibility = Visibility.Public) {
            Name = name;
            Type = type;
            Default = defaultValue;
            Visibility = visibility;
        }
    }

    public struct Parameter {
        public string Name;
        public ZenType Type;
        public ZenValue DefaultValue;

        public bool IsTypeParameter => Type == ZenType.Type;
        public bool IsValueParameter => !IsTypeParameter;

        public Parameter(string name, ZenType type, ZenValue? defaultValue = null) {
            Name = name;
            Type = type;  // Keep the original type (ZenType.Type for type parameters)
            
            if (defaultValue != null) {
                DefaultValue = (ZenValue) defaultValue;
            }
            else if (IsTypeParameter) {
                // For type parameters, default to ZenType.Any
                DefaultValue = new ZenValue(ZenType.Type, ZenType.Any);
            }
            else {
                DefaultValue = ZenValue.Null;
            }
        }

        public bool ValidateValue(ZenValue value) {
            if (IsTypeParameter) {
                // For type parameters, value must be a ZenType.Type
                // and its underlying value must be a ZenType
                return value.Type == ZenType.Type;
            }
            return Type.IsAssignableFrom(value.Type);
        }
    }

    public string Name;
    public ZenClass SuperClass = Master;
    public List<ZenMethod> Methods = [];
    public Dictionary<string, Property> Properties = [];
    public ZenTypeClass Type;
    public List<Parameter> Parameters = [];

    public ZenClass(string name, List<ZenMethod> methods, List<Property> properties, List<Parameter> parameters) {
        Name = name;
        Methods = methods;
        Properties = properties.ToDictionary(x => x.Name, x => x);
        Parameters = parameters;

        var parameterTypeList = parameters.Select(p => p.Type).ToArray();
        Type = new ZenTypeClass(this, Name, parameterTypeList);
    }

    public ZenClass(string name, List<ZenMethod> methods) : this(name, methods, [], []) {}

    public Dictionary<string, ZenType> ResolveTypeParameters(Dictionary<string, ZenValue> paramValues) {
        var substitutions = new Dictionary<string, ZenType>();
        
        foreach (var param in Parameters) {
            if (!param.IsTypeParameter) continue;
            
            if (paramValues.TryGetValue(param.Name, out var value)) {
                Logger.Instance.Debug($"Adding substitution {param.Name} -> {value.Underlying}");
                substitutions[param.Name] = (ZenType)value.Underlying!;
            }
        }
        
        return substitutions;
    }

    public ZenType SubstituteType(ZenType original, Dictionary<string, ZenType> substitutions) {
        // If this is a generic type parameter (like T), look up its substitution by name
        if (original.IsGeneric) {
            // Look up by the type's name directly
            if (substitutions.TryGetValue(original.Name, out var concrete)) {
                return concrete;
            }
        }
        
        // If it's a parametric type, substitute its parameters
        if (original.IsParametric) {
            var newParams = original.Parameters.Select(p => SubstituteType(p, substitutions)).ToArray();
            return new ZenType(original.Name, original.IsNullable, newParams);
        }
        
        return original;
    }

    public void ValidateParameters(Dictionary<string, ZenValue> paramValues) {
        foreach (Parameter param in Parameters) {
            if (!paramValues.ContainsKey(param.Name)) {
                if (param.DefaultValue.IsNull()) {
                    throw Interpreter.Error($"No value provided for parameter '{param.Name}' and it has no default value");
                }
                paramValues[param.Name] = param.DefaultValue;
            }
            else {
                var paramValue = paramValues[param.Name];
                if (!param.ValidateValue(paramValue)) {
                    var expectedType = param.IsTypeParameter ? "Type" : param.Type.ToString();
                    throw Interpreter.Error($"Invalid value for parameter '{param.Name}'. Expected {expectedType}, got {paramValue.Type}");
                }
            }
        }
    }

    public ZenObject CreateInstance(Interpreter interpreter, ZenValue[] args, Dictionary<string, ZenValue> paramValues) {
        Logger.Instance.Debug($"Creating instance of {Name}...");

        Logger.Instance.Debug("Using generic parameters:");
        foreach (var param in paramValues) {
            Logger.Instance.Debug($"{param.Key} = {param.Value}");
        }

        ValidateParameters(paramValues);

        ZenObject instance = new ZenObject(this);

        var typeSubstitutions = ResolveTypeParameters(paramValues);

        foreach (var param in paramValues) {
            instance.SetParameter(param.Key, param.Value);
        }

        instance.Type = new ZenTypeClass(this, Name, typeSubstitutions.Values.ToArray());

        foreach (var property in Properties) {
            if (property.Value.IsGeneric) {
                string genericParamName = property.Value.Type.Name;
                if (typeSubstitutions.TryGetValue(genericParamName, out var concreteType)) {
                    Logger.Instance.Debug($"Substituting property {property.Key} type from {property.Value.Type} to {concreteType}");
                    instance.Properties.Add(property.Key, new ZenValue(concreteType, property.Value.Default.Underlying));
                } else {
                    instance.Properties.Add(property.Key, property.Value.Default);
                }
            } else {
                instance.Properties.Add(property.Key, property.Value.Default);
            }
        }

        // concretize all methods that reference generic parameters
        foreach (var method in Methods) {
            bool needsConcrete = Parameters.Count > 0;

            if (needsConcrete) {
                ZenType concreteReturnType = method.ReturnType;

                if (method.ReturnTypeHint != null) {
                    TypeHint returnTypeHint = method.ReturnTypeHint!;
                    concreteReturnType = typeSubstitutions[returnTypeHint.Name];
                }
                
                var concreteParams = method.Arguments.Select(p => {
                    if (p.Type.IsGeneric) {
                        string genericParamName = p.Type.Name;
                        var concreteType = typeSubstitutions[genericParamName];
                        Logger.Instance.Debug($"Substituting parameter {p.Name} type from {p.Type} to {concreteType}");
                        return new ZenFunction.Argument(p.Name, concreteType, false);
                    }
                    return p;
                }).ToList();

                if (method is ZenHostMethod hostMethod) {
                    instance.Methods.Add(new ZenHostMethod(
                        method.Async,
                        method.Name,
                        method.Visibility,
                        concreteReturnType,
                        concreteParams,
                        hostMethod.Func,
                        hostMethod.Closure
                    ));
                }
                else if (method is ZenUserMethod userMethod) {
                    instance.Methods.Add(new ZenUserMethod(
                        method.Async,
                        method.Name,
                        method.Visibility,
                        concreteReturnType,
                        concreteParams,
                        userMethod.Block,
                        userMethod.Closure
                    ));
                }
            }
        }
        
        // find constructor
        ZenType[] argTypes = args.Select(x => x.Type).ToArray();

        HasOwnConstructor(argTypes, out var constructor);
        if (constructor == null && args.Length > 0) {
            throw Interpreter.Error("No valid constructor found for class " + Name);
        }
        
        // call constructor
        if (constructor != null) {
            var boundMethod = constructor!.Bind(instance);
            Logger.Instance.Debug($"Calling constructor with arguments: {string.Join(", ", boundMethod.Arguments.Select(p => $"{p.Name}: {p.Type}"))}");
            interpreter.CallUserFunction(boundMethod, args);
        }

        // verify non-nullable properties are set
        foreach (Property property in Properties.Values) {
            if (instance.GetProperty(property.Name).IsNull()) {
                throw Interpreter.Error("Non-nullable Property " + property.Name + " must be set in the constructor.", null, ErrorType.TypeError);
            }
        }

        // return the instance
        return instance;
    }

    public void HasOwnConstructor(ZenType[] argTypes, out ZenMethod? method) {
        HasOwnMethod(Name, ZenType.Void, argTypes, out method);
    }

    public void HasOwnMethod(string name, ZenType returnType, ZenType[] argTypes, out ZenMethod? method) {
        foreach (var m in Methods) {
            if (m.Name == name && m.ReturnType == returnType) {
                if (m.Arguments.Count != argTypes.Length) {
                    continue;
                }

                bool matching = true;
                for (int i = 0; i < argTypes.Length; i++) {
                    if (m.Arguments[i].Type != argTypes[i]) {
                        matching = false;
                        break;
                    }
                }

                if (matching) {
                    method = m;
                    return;
                }
            }
        }

        method = null;
    }

    public void HasOwnMethod(string name, out ZenMethod? method) {
        foreach (var m in Methods) {
            if (m.Name == name) {
                method = m;
                return;
            }
        }

        method = null;
    }

    public void HasMethodHierarchically(string name, ZenType returnType, ZenType[] argTypes, out ZenMethod? method) {
        HasOwnMethod(name, returnType, argTypes, out method);
        if (method != null) {
            return;
        }

        if (this == Master) {
            return;
        }

        SuperClass.HasMethodHierarchically(name, returnType, argTypes, out method);
    }

    public void HasMethodHierarchically(string name, out ZenMethod? method) {
        HasOwnMethod(name, out method);
        if (method != null) {
            return;
        }

        if (this == Master) {
            return;
        }else {
            SuperClass.HasMethodHierarchically(name, out method);
        }
    }

    public bool IsAssignableFrom(ZenClass other) {
        return this == other || this.SuperClass == other || this.SuperClass.IsAssignableFrom(other);
    }

    public bool IsSubclassOf(ZenClass other) {
        return other.IsAssignableFrom(this);
    }

    public override string ToString()
    {
        return $"Class {Name}";
    }
}
