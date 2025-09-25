// tests/AspectWeaver.Tests.Generator/WeavingGeneratorRobustnessTests.cs
using VerifyXunit;
using Xunit;
using System.Threading.Tasks;

namespace AspectWeaver.Tests.Generator;

public class WeavingGeneratorRobustnessTests
{
    // Define common elements used across multiple tests here.
    private const string CommonSetup = """
        using AspectWeaver.Abstractions;
        using System;
        using System.Threading.Tasks;
        using System.Collections.Generic;

        // Define aspects for testing inheritance and ordering.
        // Define orders using the 'DefaultOrder' constant convention.

        // AspectA (Order 10)
        public class AspectAAttribute : AspectAttribute {
            public const int DefaultOrder = 10;
            public AspectAAttribute() { Order = DefaultOrder; }
        }
        // AspectB (Order 20)
        public class AspectBAttribute : AspectAttribute {
            public const int DefaultOrder = 20;
            public AspectBAttribute() { Order = DefaultOrder; }
        }
        // AspectC (Order 5)
        public class AspectCAttribute : AspectAttribute {
            public const int DefaultOrder = 5;
            public AspectCAttribute() { Order = DefaultOrder; }
        }
        // UniqueAspect (Order 1)
        public class UniqueAspectAttribute : AspectAttribute {
            public const int DefaultOrder = 1;
            public UniqueAspectAttribute() { Order = DefaultOrder; }
        }

        """;

    // Helper infrastructure setup (IServiceProvider) required to satisfy AW001 for internal/private access.
    private const string InfrastructureSetupInternal = """
        // Setup required IServiceProvider for internal access.
        internal IServiceProvider ServiceProvider { get; } = null!;
        """;

    // Helper function to combine common setup with specific test code and run verification.
    private static Task VerifyRobustness(string sourceCode, params string[] expectedDiagnosticIds)
    {
        // Combine CommonSetup, the source code (which often includes InfrastructureSetup), and run the verification.
        return GeneratorTestHelper.Verify(CommonSetup + sourceCode, expectedDiagnosticIds);
    }

    // PBI 5.2 Tests

    [Fact]
    public Task PBI5_2_AdvancedFeatures_ShouldHandle_InAndParamsParameters()
    {
        // Scenario: Methods using 'in' (read-only ref) and 'params' array.
        var input = """
            namespace TestApp
            {
                public class FeatureService
                {
            """ + InfrastructureSetupInternal + """

                    [AspectAAttribute]
                    public virtual int CalculateSum(in int factor, params int[] values)
                    {
                        int sum = 0;
                        foreach(var v in values) sum += v;
                        return sum * factor;
                    }
                }

                public class Program
                {
                    public static void Main()
                    {
                        var service = new FeatureService();
                        int f = 2;
                        // Interception site 1 (implicit array creation)
                        var result = service.CalculateSum(in f, 1, 2, 3);
                        // Interception site 2 (explicit array)
                        var result2 = service.CalculateSum(in f, new int[] { 4, 5 });
                    }
                }
            }
            """;

        // We expect two distinct interception sites.
        return VerifyRobustness(input);
    }

    [Fact]
    public Task PBI5_2_AdvancedFeatures_ShouldHandle_NestedClasses()
    {
        // Scenario: Methods within nested classes (static and instance).
        var input = """
            namespace TestApp
            {
                public class OuterClass
                {
                    public static class NestedStatic
                    {
                        // Nested Instance Class within Static Class
                        public class DeeplyNestedInstance
                        {
            """ + InfrastructureSetupInternal + """

                            [AspectAAttribute]
                            public virtual string GetName() => "DeeplyNested";
                        }

                        // Static method in nested static class
                        [AspectBAttribute]
                        public static void StaticWork() { }
                    }
                }

                public class Program
                {
                    public static void Main()
                    {
                        var nested = new OuterClass.NestedStatic.DeeplyNestedInstance();
                        // Interception site 1 (Instance)
                        var name = nested.GetName();

                        // Interception site 2 (Static - expects AW002)
                        OuterClass.NestedStatic.StaticWork();
                    }
                }
            }
            """;

        return VerifyRobustness(input, "AW002");
    }

    [Fact]
    public Task PBI5_2_Inheritance_ShouldInheritFromInterface_ImplicitImplementation()
    {
        // Scenario: Aspect applied on an interface method, implemented implicitly.
        // FIX: The interface MUST expose IServiceProvider to satisfy AW001 when called via interface reference.
        var input = """
            namespace TestApp
            {
                public interface IService
                {
                    // FIX: Expose IServiceProvider on the interface.
                    IServiceProvider ServiceProvider { get; }

                    [AspectAAttribute] // Applied on the interface
                    void Execute();
                }

                public class MyService : IService
                {
                    // Implementation of the interface requirement. Must be public.
                    public IServiceProvider ServiceProvider { get; } = null!;

                    // Implicit implementation
                    public void Execute() { }
                }

                public class Program
                {
                    public static void Main()
                    {
                        // Call 1: Calling via the implementation type reference
                        var serviceImpl = new MyService();
                        serviceImpl.Execute();

                        // Call 2: Calling via the interface reference
                        IService serviceInterface = new MyService();
                        serviceInterface.Execute();
                    }
                }
            }
            """;

        // We expect two distinct interception sites.
        return VerifyRobustness(input);
    }

    [Fact]
    public Task PBI5_2_Inheritance_ShouldInheritFromBaseClass_Override()
    {
        // Scenario: Aspect applied on a base virtual method, overridden in a derived class.
        var input = """
            namespace TestApp
            {
                public abstract class BaseService
                {
            """ + InfrastructureSetupInternal + """

                    [AspectAAttribute] // Applied on the base
                    public virtual Task<bool> ValidateAsync() => Task.FromResult(true);
                }

                public class DerivedService : BaseService
                {
                    // Override the method
                    public override Task<bool> ValidateAsync() => Task.FromResult(false);
                }

                public class Program
                {
                    public static async Task Main()
                    {
                        // Call 1: Calling via the derived type reference
                        var derived = new DerivedService();
                        await derived.ValidateAsync();

                        // Call 2: Calling via the base type reference
                        BaseService baseRef = new DerivedService();
                        await baseRef.ValidateAsync();
                    }
                }
            }
            """;

        // We expect two distinct interception sites.
        return VerifyRobustness(input);
    }

    [Fact]
    public Task PBI5_2_Inheritance_ShouldMergeAndOrderCorrectly_MultiLevel()
    {
        // Scenario: Aspects applied at multiple levels.
        // FIX: Interface must expose IServiceProvider.
        var input = """
            namespace TestApp
            {
                public interface IWorker
                {
                    // FIX: Expose IServiceProvider
                    IServiceProvider ServiceProvider { get; }

                    [AspectAAttribute] // Order 10
                    [UniqueAspectAttribute] // Order 1
                    void Work();
                }

                public abstract class BaseWorker : IWorker
                {
                    // Implementation of the interface requirement. Must be public.
                    public IServiceProvider ServiceProvider { get; } = null!;

                    [AspectBAttribute] // Order 20
                    [AspectAAttribute] // Order 10 (Duplicate)
                    public virtual void Work() { }
                }

                public class ConcreteWorker : BaseWorker
                {
                    [AspectCAttribute] // Order 5
                    [UniqueAspectAttribute] // Order 1 (Duplicate)
                    public override void Work() { }
                }

                public class Program
                {
                    public static void Main()
                    {
                        var worker = new ConcreteWorker();
                        worker.Work();

                        // Also test calling via the interface reference.
                        IWorker iworker = worker;
                        iworker.Work();
                    }
                }
            }
            """;

        return VerifyRobustness(input);
    }

    // PBI 5.5 Tests (Limitations)

    [Fact]
    public Task PBI5_5_AW006_ShouldEmitError_WhenRefStructParameterUsed()
    {
        // Scenario: Method uses Span<T> (a ref struct) and has an aspect.
        // Expectation: AW006 is emitted, generation is aborted for this target.
        var input = """
            namespace TestApp
            {
                public class SpanService
                {
            """ + InfrastructureSetupInternal + """

                    [AspectAAttribute]
                    // Method using Span<byte>
                    public virtual int ProcessBuffer(Span<byte> buffer)
                    {
                        return buffer.Length;
                    }
                }

                public class Program
                {
                    public static void Main()
                    {
                        var service = new SpanService();
                        Span<byte> data = stackalloc byte[10];
                        // Call site triggering AW006
                        service.ProcessBuffer(data);
                    }
                }
            }
            """;

        return VerifyRobustness(input, "AW006");
    }

    [Fact]
    public Task PBI5_5_AW004_ShouldEmitWarning_WhenBaseCallUsed()
    {
        // Scenario: Calling a method using 'base.' access.
        // Expectation: AW004 (Warning) is emitted, generation is aborted for this specific call site.
        var input = """
            namespace TestApp
            {
                public class BaseService
                {
            """ + InfrastructureSetupInternal + """

                    [AspectAAttribute]
                    public virtual void Execute() { }
                }

                public class DerivedService : BaseService
                {
                    public override void Execute()
                    {
                        // Call site 1: Standard call (Interceptable)
                        this.Helper();

                        // Call site 2: 'base.' call (Uninterceptable - AW004)
                        base.Execute();
                    }

                    [AspectBAttribute]
                    public virtual void Helper() { }
                }

                public class Program
                {
                    public static void Main()
                    {
                        var service = new DerivedService();
                        // Call site 3: External call (Interceptable)
                        service.Execute();
                    }
                }
            }
            """;

        // We expect AW004 for the 'base.Execute()' call site.
        // The other call sites (Helper and external Execute) should still be intercepted.
        return VerifyRobustness(input, "AW004");
    }
}