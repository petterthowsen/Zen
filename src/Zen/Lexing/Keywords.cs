namespace Zen.Lexing;

public enum Keywords
{
    // Variable Declaration
    Var, Const,

    // Primitive Types
    Int, Int64, Float, Float64, String, Bool, Char, Any,
    
    // Primitive Constants
    True, False, Null, Void,

    // Control Flow Blocks
    If, Else, Elif, For, In, While, When,

    // Control Flow Keywords
    Continue,
    Break,
    Yield,
    Return,

    // Logical Operators
    And, Or, Not,

    // Function & Class Keywords
    Func, Async, Await,

    // Class Keywords
    Class, Extends, Implements, 
    Public, Protected, Private, 
    Super, New, Is, This, Static,
    Abstract, Final, Readonly, Override,

    Namespace, Package, Import, From, As, Alias,

    Try, Catch, Throw, Finally,

    Print, Include
}