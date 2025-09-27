namespace Aymen83.AspectWeaver.Tests.Generator;

public class WeavingGeneratorTests
{
    [Fact]
    public Task Generator_WhenInputIsEmpty_ShouldOnlyGeneratePrerequisites()
    {
        var input = """
                    // Empty input code
                    """ ;
        return GeneratorTestHelper.Verify(input);
    }

    [Fact]
    public Task ShouldIntercept_SynchronousInstanceMethod()
    {
        var input = """
            using Aymen83.AspectWeaver.Abstractions;
            using System;

            public class MyTestAspectAttribute : AspectAttribute { }

            namespace TestApp
            {
                public class MyService
                {
                    internal IServiceProvider ServiceProvider { get; } = null!;

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
                        var result = service.CalculateValue(10);
                    }
                }
            }
            """ ;

        return GeneratorTestHelper.Verify(input);
    }

    [Fact]
    public Task ShouldNotIntercept_SynchronousStaticMethod()
    {
        var input = """
            using Aymen83.AspectWeaver.Abstractions;

            public class MyTestAspectAttribute : AspectAttribute { }

            namespace TestApp
            {
                public static class StaticService
                {
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
                        StaticService.LogMessage("Hello AspectWeaver");
                    }
                }
            }
            """ ;

        return GeneratorTestHelper.Verify(input, "AW002");
    }

    [Fact]
    public Task ShouldHandle_RefAndOutParameters()
    {
        var input = """
            using Aymen83.AspectWeaver.Abstractions;
            using System;

            public class RefOutAspectAttribute : AspectAttribute { }

            namespace TestApp
            {
                public class ComplexService
                {
                    internal IServiceProvider ServiceProvider { get; } = null!;

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
                        service.TryParse("test", out val, ref init);
                    }
                }
            }
            """ ;

        return GeneratorTestHelper.Verify(input);
    }

    [Fact]
    public Task Async_ShouldIntercept_Task()
    {
        var input = """
            using Aymen83.AspectWeaver.Abstractions;
            using System.Threading.Tasks;
            using System;

            public class AsyncAspectAttribute : AspectAttribute { }

            namespace TestApp
            {
                public class AsyncService
                {
                    internal IServiceProvider ServiceProvider { get; } = null!;

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
                        await service.DoWorkAsync();
                    }
                }
            }
            """ ;

        return GeneratorTestHelper.Verify(input);
    }

    [Fact]
    public Task Async_ShouldIntercept_ValueTask()
    {
        var input = """
            using Aymen83.AspectWeaver.Abstractions;
            using System.Threading.Tasks;
            using System;

            public class AsyncAspectAttribute : AspectAttribute { }

            namespace TestApp
            {
                public class AsyncService
                {
                    internal IServiceProvider ServiceProvider { get; } = null!;

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
                        await service.DoWorkValueAsync();
                    }
                }
            }
            """ ;

        return GeneratorTestHelper.Verify(input);
    }

    [Fact]
    public Task Async_ShouldIntercept_TaskOfT()
    {
        var input = """
            using Aymen83.AspectWeaver.Abstractions;
            using System.Threading.Tasks;
            using System;

            public class AsyncAspectAttribute : AspectAttribute { public AsyncAspectAttribute(string config) {} }

            namespace TestApp
            {
                public class AsyncService
                {
                    internal IServiceProvider ServiceProvider { get; } = null!;

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
                        int result = await service.CalculateAsync();
                    }
                }
            }
            """ ;

        return GeneratorTestHelper.Verify(input);
    }

    [Fact]
    public Task Async_ShouldIntercept_ValueTaskOfT()
    {
        var input = """
            using Aymen83.AspectWeaver.Abstractions;
            using System.Threading.Tasks;
            using System;

            public class AsyncAspectAttribute : AspectAttribute { }

            namespace TestApp
            {
                public class AsyncService
                {
                    internal IServiceProvider ServiceProvider { get; } = null!;

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
                        string result = await service.FetchDataValueAsync();
                    }
                }
            }
            """ ;

        return GeneratorTestHelper.Verify(input);
    }

    [Fact]
    public Task Generics_ShouldIntercept_MethodInGenericClass_NonGenericContext()
    {
        // Scenario: Calling a method in a generic class with specific types.
        var input = """
            using Aymen83.AspectWeaver.Abstractions;
            using System.Collections.Generic;
            using System;

            public class GenericAspectAttribute : AspectAttribute { }

            namespace TestApp
            {
                public class Repository<TEntity> where TEntity : class
                {
                    internal IServiceProvider ServiceProvider { get; } = null!;

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
                        var user = repo.GetById(1);
                    }
                }
            }
            """ ;

        return GeneratorTestHelper.Verify(input);
    }

    [Fact]
    public Task Generics_ShouldIntercept_GenericMethod_NonGenericContext()
    {
        // Scenario: Calling a generic method with specific types.
        var input = """
            using Aymen83.AspectWeaver.Abstractions;
            using System;

            public class GenericAspectAttribute : AspectAttribute { }

            namespace TestApp
            {
                public class UtilityService
                {
                    internal IServiceProvider ServiceProvider { get; } = null!;

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
                        var result = service.Convert<string, int>("123");
                    }
                }
            }
            """ ;

        return GeneratorTestHelper.Verify(input);
    }

    [Fact]
    public Task Generics_ShouldIntercept_GenericMethod_GenericContext()
    {
        // Scenario: Calling a generic method within a generic context.
        var input = """
            using Aymen83.AspectWeaver.Abstractions;
            using System;

            public class GenericAspectAttribute : AspectAttribute { }

            namespace TestApp
            {
                public class UtilityService
                {
                    internal IServiceProvider ServiceProvider { get; } = null!;

                    [GenericAspectAttribute]
                    public virtual T Echo<T>(T input) => input;
                }

                public class Wrapper
                {
                    public T Wrap<T>(T value)
                    {
                        var service = new UtilityService();
                        return service.Echo(value);
                    }
                }
            }
            """ ;

        return GeneratorTestHelper.Verify(input);
    }

    [Fact]
    public Task Generics_ShouldIntercept_GenericMethodWithConstraints()
    {
        // Scenario: Calling a generic method with constraints within a compatible generic context.
        var input = """
            using Aymen83.AspectWeaver.Abstractions;
            using System;

            public class GenericAspectAttribute : AspectAttribute { }

            namespace TestApp
            {
                public class ConstrainedService
                {
                    internal IServiceProvider ServiceProvider { get; } = null!;

                    [GenericAspectAttribute]
                    public virtual T Process<T>(T input)
                        where T : class, IDisposable, new()
                    {
                        return input;
                    }
                }

                public class Wrapper
                {
                    public T WrapProcess<T>(T resource) where T : class, IDisposable, new()
                    {
                         var service = new ConstrainedService();
                        return service.Process(resource);
                    }
                }
            }
            """ ;

        return GeneratorTestHelper.Verify(input);
    }

    [Fact]
    public Task Generics_ShouldIntercept_AsyncGenericMethod()
    {
        // Scenario: Combining Async and Generics in a generic context.
        var input = """
            using Aymen83.AspectWeaver.Abstractions;
            using System.Threading.Tasks;
            using System;

            public class GenericAspectAttribute : AspectAttribute { }

            namespace TestApp
            {
                public class AsyncGenericService
                {
                    internal IServiceProvider ServiceProvider { get; } = null!;

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
                        return await service.FetchAsync<T>(key);
                    }
                }
            }
            """ ;

        return GeneratorTestHelper.Verify(input);
    }
}