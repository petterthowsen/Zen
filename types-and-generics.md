# Types & Generics

Zen needs to support "generics". Also known as "parametric types". I.E, a Class that can be instantiated with parameters.

## Background
A bit of background on how variables and types are represented currently.
Types are represented by a ZenType. There are a bunch of static members on ZenType for the primitives (I.E ZenType.String, ZenType.Integer and ZenType.Type).
Values are represented by a ZenValue, which wraps a ZenType and a underlying value (dynamic).

All the primitives are builtin global variables and we also have a builtin type() function that returns the type of any given value.

For example `type("hello")` returns a ZenValue of type ZenType.Type whose underlying value == ZenType.String. In other words, it returns a "Type" type. A bit confusing.

In other words, one can do `type("hello") == type("world")` which would == true.
We also have a `is` operator to simplify this, so `"hello" is string` is equivalently == true.

### Type Hints
Type hints, such as `int' is parsed and stored as a TypeHint expr node. The VarStmt for example, may parse an expression after : as a TypeHint.

## Workflow

Given the following code:
```zen
class Generic<T:Type> {
    instance:T
    Generic(inst:T) {
        this.instance = inst
    }
}
```

### Parsing ClassStmt
The parser will parse that as a ClassStmt Node.
The ClassStmt node will store the parameters (in this case "T") as a list of Parameter expr nodes.

### Executing ClassStmt
When the Interpreter executes a ClassStmt, we create a ZenClass.

When a ZenClass is created, it creates a variable called "Generic" and maps that to a ZenValue of type ZenType.Class whose underling is that ZenClass instance.
We also set zenClass.Type = new ZenType(className, ZenType.Class)
This will make the builtin type() function work with classes - it'll check if the value is an instance of a class and if so return the zenClass.Type.

When the ClassStmt parsing function encounters references to a parameter like 'T', we need to tag that as a special generic parameter.
We can do this by having the parsing function for ClassStmts pass a list of parameters to the TypeHint() parser function. Like `typeExpr = TypeHint(["T"])
The TypeHint parsing function then checks to see if we encounter a 'T' and in this case, instead of returning a TypeHint expr that assumes T is some known type, we return one where that TypeHint is generic, suggesting it should be resolved to some concrete type when evaluated.

When TypeHint expr is evaluated, generally it will simply check the name, for example `string` and using LookupVariable, it will return a TypeResult (a version of IEvaluationResult):

```csharp
// from Interpreter.ExprVisitor.cs, in Visit(TypeHint typeHint):
VariableResult variable = LookUpVariable(typeHint.Name, typeHint);
if (variable.Type == ZenType.Class) {
    ZenClass clazz = (ZenClass)variable.Value.Underlying!;
    return new TypeResult(clazz.Type);
}else {
    return new TypeResult(variable.Type);
}
```

### Parsing Instantiation expressions
Given the syntax:
```zen
var g = new Generic<string>("Hello")
```

It is parsed as an Instantiation expression node.
The Instantiation node records the 'New' token, a 'Call' expr (the 'Generic' identifier in this case) and any parameters between <>, in this case the identifier "string".

The parameters are parsed similarly to function arguments, where we can pass in 'string'.

### Executing Instantiations

For a generic class to work, we need to create concrete version of the methods that happen to refer to any of the generic parameters.
When we do this, we substitute those generic parameters with their parameter values that is passed into the instantiation via new Generic<param1, param2>().

In addition, when a method is bound, before calling, we define and assign variables of all the generic parameters to their concrete values. Making them available inside the method body.

## Questions
type() builtin function returns the type of the given argument... 
We need to make sure that
type(myGenericInstance) returns a ZenValue of type ZenType.Type whose value is the underlying ZenClass. Assuming myGenericInstance is an instance of Generic<T> from bove.
type("string"), type(int) is already working correctly, as do type-casting like `(int) 2.5 == 2`.

What happens if we do type(Generic) ? since Generic is already a type. I suppose we return a ZenType.Type with a value of ZenTypeClass ?

### TypeHint.GetZenType and TypeHint.GetBaseZenType
Currently, we rely on ZenType.FromString - but this only works for the primitives - not for user-defined types.
We should change remove this logic and have the consumer of the TypeHint be responsible for resolving the type name to some known type.
This makes more sense on multiple levels, esp regarding generics.
In fact, the Interpreter.Visit(TypeHint) already does this. It simply reads the name of the TypeHint and looks up the variable.