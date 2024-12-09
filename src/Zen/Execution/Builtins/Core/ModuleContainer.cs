
using Zen.Typing;

namespace Zen.Execution.Builtins.Core;

public class ModuleContainer : IBuiltinsProvider
{

    public static ZenClass Clazz;

    public static async Task RegisterBuiltins(Interpreter interp)
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

        env.Define(true, "Module", ZenType.Class, false);
        env.Assign("Module", new ZenValue(ZenType.Class, Clazz));

        await Task.CompletedTask;
    }
}