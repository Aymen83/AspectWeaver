using VerifyXunit;
using Xunit;
using System.Threading.Tasks;

namespace AspectWeaver.Tests.Generator;

public class WeavingGeneratorTests
{
    [Fact]
    public Task Generator_WhenInputIsEmpty_ShouldOnlyGeneratePrerequisites()
    {
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
                    // Use full attribute name
                    [MyTestAspectAttribute]
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
                    // Use full attribute name
                    [MyTestAspectAttribute]
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
                    // Use full attribute name
                    [RefOutAspectAttribute]
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

    // PBI 2.6 Tests

    [Fact]
    public Task PBI2_6_Async_ShouldIntercept_Task()
    {
        var input = """
            using AspectWeaver.Abstractions;
            using System.Threading.Tasks;

            public class AsyncAspectAttribute : AspectAttribute { }

            namespace TestApp
            {
                public class AsyncService
                {
                    // Use full attribute name
                    [AsyncAspectAttribute]
                    public virtual Task DoWorkAsync()
                    {
                        return Task.CompletedTask;
                    }
                }

                public class Program
                {
                    public static async Task Main()
                    {
                        var service = new AsyncService();
                        // Interception site
                        await service.DoWorkAsync();
                    }
                }
            }
            """;

        return GeneratorTestHelper.Verify(input);
    }

    [Fact]
    public Task PBI2_6_Async_ShouldIntercept_ValueTask()
    {
        var input = """
            using AspectWeaver.Abstractions;
            using System.Threading.Tasks;

            public class AsyncAspectAttribute : AspectAttribute { }

            namespace TestApp
            {
                public class AsyncService
                {
                    // Use full attribute name
                    [AsyncAspectAttribute]
                    public virtual ValueTask DoWorkValueAsync()
                    {
                        return default;
                    }
                }

                public class Program
                {
                    public static async Task Main()
                    {
                        var service = new AsyncService();
                        // Interception site
                        await service.DoWorkValueAsync();
                    }
                }
            }
            """;

        return GeneratorTestHelper.Verify(input);
    }

    [Fact]
    public Task PBI2_6_Async_ShouldIntercept_TaskOfT()
    {
        var input = """
            using AspectWeaver.Abstractions;
            using System.Threading.Tasks;

            // Testing attribute constructor rehydration
            public class AsyncAspectAttribute : AspectAttribute { public AsyncAspectAttribute(string config) {} }

            namespace TestApp
            {
                public class AsyncService
                {
                    // Use full attribute name
                    [AsyncAspectAttribute("ConfigValue", Order = 5)]
                    public virtual Task<int> CalculateAsync()
                    {
                        return Task.FromResult(42);
                    }
                }

                public class Program
                {
                    public static async Task Main()
                    {
                        var service = new AsyncService();
                        // Interception site
                        int result = await service.CalculateAsync();
                    }
                }
            }
            """;

        return GeneratorTestHelper.Verify(input);
    }

    [Fact]
    public Task PBI2_6_Async_ShouldIntercept_ValueTaskOfT()
    {
        var input = """
            using AspectWeaver.Abstractions;
            using System.Threading.Tasks;

            public class AsyncAspectAttribute : AspectAttribute { }

            namespace TestApp
            {
                public class AsyncService
                {
                    // Testing attribute property rehydration
                    // Use full attribute name
                    [AsyncAspectAttribute(Order = 10)]
                    public virtual ValueTask<string> FetchDataValueAsync()
                    {
                        return new ValueTask<string>("Data");
                    }
                }

                public class Program
                {
                    public static async Task Main()
                    {
                        var service = new AsyncService();
                        // Interception site
                        string result = await service.FetchDataValueAsync();
                    }
                }
            }
            """;

        return GeneratorTestHelper.Verify(input);
    }
}