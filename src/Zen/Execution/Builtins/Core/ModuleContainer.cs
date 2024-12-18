
using Zen.Typing;

namespace Zen.Execution.Builtins.Core;

public class ModuleContainer : IBuiltinsProvider
{

    public static ZenClass Clazz;

    public static async Task Initialize(Interpreter interp)
    {
        Environment env = interp.globalEnvironment;
        
        Clazz = new ZenClass("Module",
            // methods
            [

            ],
            
            // properties
            [
                
            ],

            // parameters
            [
                
            ]
        );

        env.Define(true, "Module", ZenType.Type, false);
        env.Assign("Module", new ZenValue(ZenType.Type, Clazz.Type));

        await Task.CompletedTask;
    }

    public static async Task Register(Interpreter interp)
    {
        await Task.CompletedTask;
    }
}