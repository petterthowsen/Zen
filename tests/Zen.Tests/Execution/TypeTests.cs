using Xunit;
using Xunit.Abstractions;

namespace Zen.Tests.Execution
{
    public class TypeTests : TestRunner
    {
        public TypeTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void TestValueIsType()
        {
            RestartInterpreter();

            Execute("var a = 5");
            string? result = Execute("print a is int");
            Assert.Equal("true", result);

            Execute("var b:int? = 5");
            result = Execute("print b is int?");
            Assert.Equal("true", result);

            result = Execute("print b is int");
            Assert.Equal("true", result);

            Execute("var c:int? = null");
            result = Execute("print c is int");
            Assert.Equal("false", result);
        }

        [Fact]
        public void TestTypeIsType()
        {
            RestartInterpreter();

            string? result = Execute("print int is int");
            Assert.Equal("true", result);

            result = Execute("print int? is int?");
            Assert.Equal("true", result);

            result = Execute("print int is int?");
            Assert.Equal("true", result);

            result = Execute("print int? is int");
            Assert.Equal("false", result);
        }
    }
}
