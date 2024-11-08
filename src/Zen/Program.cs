namespace Zen;

using Zen.Lexing;

public class Program
{
    public static void Main(string[] args)
    {
        var lexer = new Lexer();
        Console.WriteLine(lexer.hello());
    }
}
