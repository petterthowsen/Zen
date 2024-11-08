using Xunit;
using Zen.Lexing;

namespace Zen.Tests;

public class LexerTests
{
    [Fact]
    public void HelloTest()
    {
        var lexer = new Lexer();
        Assert.Equal("hello, world", lexer.hello());
    }
}
