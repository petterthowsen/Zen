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

    // Function & Class Keywords
    Func, Async, Await,
    
    // Class Keywords
    Class, Extends, Implements, Abstract, Public, Private, Super, New, Is, This,

    Namespace, Import, From, As, Alias,

    Try, Catch, Throw, Finally,

    Echo,
}