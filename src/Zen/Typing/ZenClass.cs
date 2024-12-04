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

    public List<ZenInterface> Interfaces = [];

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

        // validate the generic parameters
        ValidateParameters(paramValues);
        
        // create the instance
        ZenObject instance = new(this);
        
        // store the concrete parameter values on the instance
        foreach (var param in paramValues) {
            instance.SetParameter(param.Key, param.Value);
        }
        
        // todo: remove this, we're already setting these on the instance.Parameters
        var typeSubstitutions = ResolveTypeParameters(paramValues);

        // create the concrete type, I.E ClassName<T> becomes ClassName<string> etc.
        instance.Type = new ZenTypeClass(this, Name, [..typeSubstitutions.Values]);

        // set properties
        foreach (Property property in Properties.Values) {
            if (property.Type.IsGeneric) {
                string genericParamName = property.Type.Name;
                if (instance.HasParameter(genericParamName)) {
                    Logger.Instance.Debug($"Property '{property.Name}' uses generic type '{genericParamName}'.");
                    
                    // resolve the concrete type
                    ZenType concreteType = instance.GetParameter(genericParamName).Underlying!;
                    
                    Logger.Instance.Debug($"Property {property.Name} substituted from generic {genericParamName} to {concreteType}");
                    instance.Properties.Add(property.Name, new ZenValue(concreteType, property.Default.Underlying));
                }else {
                    throw Interpreter.Error($"Property {property.Name} uses an unknown parametric type {property.Type}. This sholdn't happen!");
                }
            }else {
                Logger.Instance.Debug($"Property '{property.Name}' is just a {property.Type}.");
                instance.Properties.Add(property.Name, property.Default);
            }
        }

        // concretize all methods that reference generic parameters
        foreach (var method in Methods) {
            bool needsConcrete = Parameters.Count > 0;

            if (needsConcrete) {
                Logger.Instance.Debug($"Concretizing {method.Name}...");
                // todo: fix this
                ZenType concreteReturnType = method.ReturnType;

                if (method.ReturnTypeHint != null && method.ReturnTypeHint.IsGeneric) {
                    TypeHint returnTypeHint = method.ReturnTypeHint!;
                    concreteReturnType = instance.GetParameter(returnTypeHint.Name).Type;
                }
                
                var concreteArguments = method.Arguments.Select(arg => {
                    if (arg.Type.IsGeneric) {
                        string genericParamName = arg.Type.Name;
                        
                        ZenType concreteType = instance.GetParameter(genericParamName).Underlying!;

                        Logger.Instance.Debug($"Substituting parameter {arg.Name} from {arg.Type} to {concreteType}");
                        return new ZenFunction.Argument(arg.Name, concreteType, false);
                    }else {
                        Logger.Instance.Debug($"Parameter {arg.Name} is just a {arg.Type}.");
                    }
                    return arg;
                }).ToList();

                if (method is ZenHostMethod hostMethod) {
                    instance.Methods.Add(new ZenHostMethod(
                        method.Async,
                        method.Name,
                        method.Visibility,
                        concreteReturnType,
                        concreteArguments,
                        hostMethod.Func
                    ));
                }
                else if (method is ZenUserMethod userMethod) {
                    instance.Methods.Add(new ZenUserMethod(
                        method.Async,
                        method.Name,
                        method.Visibility,
                        concreteReturnType,
                        concreteArguments,
                        userMethod.Block,
                        userMethod.Closure
                    ));
                }
            }
        }
        
        // find constructor: note that we access the instance here, to make sure we also check the concretized versions if any.
        // it'll fallback to class methods for non-generic methods.
        var constructor = instance.GetOwnConstructor(args);
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

    public bool HasOwnConstructor(ZenValue[] argValues) {
        return GetOwnMethod(Name, argValues) != null;
    }

    public virtual ZenMethod? GetOwnConstructor(ZenValue[] argValues)
    {
        return GetOwnMethod(Name, argValues, ZenType.Void);
    }

    public virtual ZenMethod? GetOwnMethod(string name, ZenValue[] argValues, ZenType? returnType = null) {
        var argTypes = argValues.Select(x => x.Type).ToArray();
        foreach (var m in Methods) {
            if (m.Name != name) continue;

            if (returnType != null && false == TypeChecker.IsCompatible(returnType, m.ReturnType)) {
                continue;
            }

            if (m.Arguments.Count != argTypes.Length) {
                continue;
            }

            bool matching = true;
            for (int i = 0; i < argTypes.Length; i++) {

                if (false == TypeChecker.IsCompatible(argTypes[i], m.Arguments[i].Type)) {
                    matching = false;
                    break;
                }
            }

            if (matching) {
                return m;
            }
        }

        return null;
    }

    public virtual ZenMethod? GetOwnMethod(string name)
    {
        foreach (var m in Methods) {
            if (m.Name == name) {
                return m;
            }
        }

        return null;
    }

    public bool HasOwnMethod(string name, ZenValue[] argValues, ZenType? returnType = null) {
        return GetOwnMethod(name, argValues, returnType) != null;
    }

    public bool HasOwnMethod(string name) {
        foreach (var m in Methods) {
            if (m.Name == name) {
                return true;
            }
        }

        return false;
    }

    public bool HasMethodHierarchically(string name, ZenValue[] argValues, ZenType? returnType = null) {
        if (HasOwnMethod(name, argValues, returnType)) return true;

        if (this == Master) {
            return false;
        }

        return SuperClass.HasMethodHierarchically(name, argValues, returnType);
    }

    public bool HasMethodHierarchically(string name) {
        if (HasOwnMethod(name)) return true;

        if (this == Master) {
            return false;
        }else {
            return SuperClass.HasMethodHierarchically(name);
        }
    }
    
    public ZenMethod? GetMethodHierarchically(string name, ZenValue[] argValues, ZenType? returnType)
    {
        var method = GetOwnMethod(name, argValues, returnType);
        if (method != null) {
            return method;
        }

        if (this == Master) {
            return null;
        }else {
            return SuperClass.GetMethodHierarchically(name, argValues, returnType);
        }
    }

    /// <summary>
    /// Returns a concrete method that satisfies the given abstract signature.
    /// </summary>
    /// <remarks>
    /// Note that we pass types to TypeChecker in the opposite order than in GetOwnMethod.
    /// This is because we're not checking whether the argTypes are compatible with the method signature
    /// but rather whether the method signature is compatible with the argTypes.
    /// </remarks>
    protected virtual ZenMethod? GetOwnMethodSatisfying(string name, ZenType[] argTypes, ZenType? returnType)
    {
        foreach (var m in Methods) {
            if (m.Name != name) continue;

            if (returnType != null && false == TypeChecker.IsCompatible(m.ReturnType, returnType)) {
                continue;
            }

             if (m.Arguments.Count != argTypes.Length) {
                continue;
            }

            bool matching = true;
            for (int i = 0; i < argTypes.Length; i++) {

                if (false == TypeChecker.IsCompatible(m.Arguments[i].Type, argTypes[i])) {
                    matching = false;
                    break;
                }
            }

            if (matching) {
                return m;
            }
        }

        return null;
    }

    protected ZenMethod? GetMethodSatisfying(string name, ZenType[] argTypes, ZenType? returnType)
    {
        var method = GetOwnMethodSatisfying(name, argTypes, returnType);
        if (method != null) {
            return method;
        }

        if (this == Master) {
            return null;
        }else {
            return SuperClass.GetOwnMethodSatisfying(name, argTypes, returnType);
        }
    }

    public ZenMethod? GetMethodHierarchically(string name)
    {
        var method = GetOwnMethod(name);
        if (method != null) {
            return method;
        }

        if (this == Master) {
            return null;
        }else {
            return SuperClass.GetMethodHierarchically(name);
        }
    }

    public bool IsAssignableFrom(ZenClass other) {
        return this == other || SuperClass == other || SuperClass.IsAssignableFrom(other);
    }

    public bool Implements(ZenInterface @interface)
    {
        if (Interfaces.Contains(@interface)) {
            return true;
        }

        if (this == Master) {
            return false;
        }else {
            return SuperClass.Implements(@interface);
        }
    }

    public bool IsSubclassOf(ZenClass other) {
        return other.IsAssignableFrom(this);
    }

    public void Validate() {
        // make sure all interfaces are satisfied
        foreach (ZenInterface @interface in Interfaces) {
            foreach(ZenAbstractMethod abstractMethod in @interface.Methods) {
                ZenMethod? concreteMethod = GetMethodSatisfying(abstractMethod.Name, abstractMethod.Arguments.Select(x => x.Type).ToArray(), abstractMethod.ReturnType);
                if (concreteMethod == null) {
                    throw Interpreter.Error("Class " + Name + " does not implement interface method " + abstractMethod);
                }
            }
        }
    }

    public override string ToString()
    {
        return $"Class {Name}";
    }
}
