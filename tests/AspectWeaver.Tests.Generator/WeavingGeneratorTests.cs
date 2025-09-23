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

    // PBI 2.7 Tests (Generics)

    [Fact]
    public Task PBI2_7_Generics_ShouldIntercept_MethodInGenericClass_NonGenericContext()
    {
        // Scenario: Calling a method in a generic class with specific types.
        // Expectation: Interceptor is non-generic, 'this' parameter is specialized (e.g., Repository<User>).
        var input = """
            using AspectWeaver.Abstractions;
            using System.Collections.Generic;

            public class GenericAspectAttribute : AspectAttribute { }

            namespace TestApp
            {
                // Generic Class
                public class Repository<TEntity> where TEntity : class
                {
                    [GenericAspectAttribute]
                    public virtual TEntity GetById(int id)
                    {
                        return null!;
                    }
                }

                public class User { }

                public class Program
                {
                    public static void Main()
                    {
                        var repo = new Repository<User>();
                        // Interception site (Non-generic context)
                        var user = repo.GetById(1);
                    }
                }
            }
            """;

        return GeneratorTestHelper.Verify(input);
    }

    [Fact]
    public Task PBI2_7_Generics_ShouldIntercept_GenericMethod_NonGenericContext()
    {
        // Scenario: Calling a generic method with specific types.
        // Expectation: Interceptor is non-generic, specialized to the types used (e.g., int Convert(string input)).
        var input = """
            using AspectWeaver.Abstractions;

            public class GenericAspectAttribute : AspectAttribute { }

            namespace TestApp
            {
                public class UtilityService
                {
                    // Generic Method
                    [GenericAspectAttribute]
                    public virtual TResult Convert<TInput, TResult>(TInput input)
                    {
                        return default!;
                    }
                }

                public class Program
                {
                    public static void Main()
                    {
                        var service = new UtilityService();
                        // Interception site (Non-generic context, specific types)
                        var result = service.Convert<string, int>("123");
                    }
                }
            }
            """;

        return GeneratorTestHelper.Verify(input);
    }

    [Fact]
    public Task PBI2_7_Generics_ShouldIntercept_GenericMethod_GenericContext()
    {
        // Scenario: Calling a generic method within a generic context.
        // Expectation: Interceptor MUST be generic and replicate the context (e.g., T InterceptMethod<T>(...)).
        var input = """
            using AspectWeaver.Abstractions;

            public class GenericAspectAttribute : AspectAttribute { }

            namespace TestApp
            {
                public class UtilityService
                {
                    [GenericAspectAttribute]
                    public virtual T Echo<T>(T input) => input;
                }

                public class Wrapper
                {
                    public T Wrap<T>(T value)
                    {
                        var service = new UtilityService();
                        // Interception site (Generic context, types are generic parameters)
                        return service.Echo(value);
                    }
                }
            }
            """;

        return GeneratorTestHelper.Verify(input);
    }

    [Fact]
    public Task PBI2_7_Generics_ShouldIntercept_GenericMethodWithConstraints()
    {
        // Scenario: Calling a generic method with constraints within a compatible generic context.
        // Expectation: Interceptor MUST replicate the constraints (e.g., where T : class, IDisposable, new()).
        var input = """
            using AspectWeaver.Abstractions;
            using System;

            public class GenericAspectAttribute : AspectAttribute { }

            namespace TestApp
            {
                public class ConstrainedService
                {
                    // Generic Method with complex constraints
                    [GenericAspectAttribute]
                    public virtual T Process<T>(T input)
                        where T : class, IDisposable, new()
                    {
                        return input;
                    }
                }

                public class Wrapper
                {
                    // Call site is within a generic method that satisfies the constraints
                    public T WrapProcess<T>(T resource) where T : class, IDisposable, new()
                    {
                         var service = new ConstrainedService();
                        // Interception site (Generic context with constraints)
                        return service.Process(resource);
                    }
                }
            }
            """;

        return GeneratorTestHelper.Verify(input);
    }

    [Fact]
    public Task PBI2_7_Generics_ShouldIntercept_AsyncGenericMethod()
    {
        // Scenario: Combining Async and Generics in a generic context.
        // Expectation: Interceptor is async, generic, and includes constraints.
        var input = """
            using AspectWeaver.Abstractions;
            using System.Threading.Tasks;

            public class GenericAspectAttribute : AspectAttribute { }

            namespace TestApp
            {
                public class AsyncGenericService
                {
                    [GenericAspectAttribute]
                    public virtual async Task<T> FetchAsync<T>(string key) where T : struct
                    {
                        await Task.Yield();
                        return default(T);
                    }
                }

                public class Wrapper
                {
                     public async Task<T> WrapFetch<T>(string key) where T : struct
                    {
                        var service = new AsyncGenericService();
                        // Interception site (Async Generic context)
                        // Note: We use the explicit generic argument <T> here for clarity.
                        return await service.FetchAsync<T>(key);
                    }
                }
            }
            """;

        return GeneratorTestHelper.Verify(input);
    }
}