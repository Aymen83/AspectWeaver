using VerifyXunit;
using Xunit;
using System.Threading.Tasks;

namespace AspectWeaver.Tests.Generator;

public class WeavingGeneratorTests
{
    [Fact]
    public Task Generator_WhenInputIsEmpty_ShouldOnlyGeneratePrerequisites()
    {
        // (Existing test remains)
        var input = """
                    // Empty input code
                    """;
        return GeneratorTestHelper.Verify(input);
    }

    [Fact]
    public Task PBI2_4_PoC_ShouldIntercept_SynchronousInstanceMethod()
    {
        // Arrange
        var input = """
            using AspectWeaver.Abstractions;

            // Define a dummy aspect for testing purposes
            public class MyTestAspectAttribute : AspectAttribute { }

            namespace TestApp
            {
                public class MyService
                {
                    [MyTestAspect]
                    public virtual int CalculateValue(int input, string prefix = "A")
                    {
                        return input * 2;
                    }
                }

                public class Program
                {
                    public static void Main()
                    {
                        var service = new MyService();
                        // This is the invocation site we expect to intercept.
                        var result = service.CalculateValue(10);
                    }
                }
            }
            """;

        // Act & Assert
        return GeneratorTestHelper.Verify(input);
    }

    [Fact]
    public Task PBI2_4_PoC_ShouldIntercept_SynchronousStaticMethod()
    {
        // Arrange
        var input = """
            using AspectWeaver.Abstractions;

            public class MyTestAspectAttribute : AspectAttribute { }

            namespace TestApp
            {
                public static class StaticService
                {
                    [MyTestAspect]
                    public static void LogMessage(string message)
                    {
                        System.Console.WriteLine(message);
                    }
                }

                public class Program
                {
                    public static void Main()
                    {
                        // This is the invocation site we expect to intercept.
                        StaticService.LogMessage("Hello AspectWeaver");
                    }
                }
            }
            """;

        // Act & Assert
        return GeneratorTestHelper.Verify(input);
    }

    [Fact]
    public Task PBI2_4_PoC_ShouldHandle_RefAndOutParameters()
    {
        var input = """
            using AspectWeaver.Abstractions;

            public class RefOutAspectAttribute : AspectAttribute { }

            namespace TestApp
            {
                public class ComplexService
                {
                    [RefOutAspect]
                    public bool TryParse(string input, out int value, ref bool initialized)
                    {
                        value = 42;
                        initialized = true;
                        return true;
                    }
                }

                public class Caller
                {
                    public void Execute()
                    {
                        var service = new ComplexService();
                        bool init = false;
                        int val;
                        // Interception site
                        service.TryParse("test", out val, ref init);
                    }
                }
            }
            """;

        return GeneratorTestHelper.Verify(input);
    }
}