using Zen.Execution;
using Zen.Parsing.AST;
using Environment = Zen.Execution.Environment;

namespace Zen.Typing;

public class ZenUserMethod : ZenMethod
{

    public Block Block;

    public ZenUserMethod(bool async, string name, ZenClass.Visibility visibility, ZenType returnType, List<Argument> arguments, Block block, Environment closure) : base(async, name, visibility, returnType, arguments)
    {
        Block = block;
        Closure = closure;
    }

    public override ZenValue Call(Interpreter interpreter, ZenValue[] arguments)
    {
        throw new Exception("User Methods cannot be called directly.");
    }
}