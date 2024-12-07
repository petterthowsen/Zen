using Zen.Execution;
using Zen.Parsing.AST;
using Environment = Zen.Execution.Environment;

namespace Zen.Typing;

public class ZenUserMethod : ZenMethod
{

    public Block Block;
    
    public ZenUserMethod(bool async, string name, ZenClass.Visibility visibility, ZenType returnType, List<Argument> arguments, Block block, Environment closure, bool @static = false) : base(async, name, visibility, returnType, arguments, @static)
    {
        Block = block;
        Closure = closure;
    }

    public override Task<ZenValue> Call(Interpreter interpreter, ZenValue[] arguments)
    {
        throw new Exception("User Methods cannot be called directly.");
    }
}