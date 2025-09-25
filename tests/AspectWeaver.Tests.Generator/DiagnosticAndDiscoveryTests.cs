namespace AspectWeaver.Tests.Generator;

public class DiagnosticAndDiscoveryTests
{
    private const string AW001 = "AW001";
    private const string AW002 = "AW002";

    // Success Cases (Discovery)

    [Fact]
    public Task PBI3_2_Discovery_ShouldSucceed_WhenPublicProperty_ServiceProvider()
    {
        var input = """
            using AspectWeaver.Abstractions;
            using System;
            public class DIAttribute : AspectAttribute { }

            namespace TestApp
            {
                public class MyService
                {
                    // Accessible provider (Prioritized Name)
                    public IServiceProvider ServiceProvider { get; } = null!;

                    [DIAttribute]
                    public void DoWork() { }
                }

                public class Program
                {
                    public static void Main()
                    {
                        var service = new MyService();
                        service.DoWork(); // Success expected.
                    }
                }
            }
            """;

        // We expect no diagnostics.
        return GeneratorTestHelper.Verify(input);
    }

    [Fact]
    public Task PBI3_2_Discovery_ShouldSucceed_WhenInternalField_Underscore()
    {
        var input = """
            using AspectWeaver.Abstractions;
            using System;
            public class DIAttribute : AspectAttribute { }

            namespace TestApp
            {
                public class MyService
                {
                    // Accessible provider (Internal field)
                    internal IServiceProvider _serviceProvider = null!;

                    [DIAttribute]
                    public void DoWork() { }
                }

                public class Program
                {
                    public static void Main()
                    {
                        var service = new MyService();
                        service.DoWork(); // Success expected.
                    }
                }
            }
            """;

        return GeneratorTestHelper.Verify(input);
    }

    [Fact]
    public Task PBI3_2_Discovery_ShouldSucceed_WhenInherited()
    {
        var input = """
            using AspectWeaver.Abstractions;
            using System;
            public class DIAttribute : AspectAttribute { }

            namespace TestApp
            {
                public class BaseService
                {
                    // Accessible provider on base class.
                    public IServiceProvider Services { get; } = null!;
                }

                public class MyService : BaseService
                {
                    [DIAttribute]
                    public void DoWork() { }
                }

                public class Program
                {
                    public static void Main()
                    {
                        var service = new MyService();
                        service.DoWork(); // Success expected.
                    }
                }
            }
            """;

        return GeneratorTestHelper.Verify(input);
    }

    // Failure Cases (Diagnostics)

    [Fact]
    public Task PBI3_2_AW001_ShouldEmitError_WhenServiceProviderNotFound()
    {
        var input = """
            using AspectWeaver.Abstractions;
            using System;
            public class DIAttribute : AspectAttribute { }

            namespace TestApp
            {
                public class MyService
                {
                    // No IServiceProvider member is present.
                    [DIAttribute]
                    public void DoWork() { }
                }

                public class Program
                {
                    public static void Main()
                    {
                        var service = new MyService();
                        // AW001 should be emitted here.
                        service.DoWork();
                    }
                }
            }
            """;

        // We expect AW001.
        return GeneratorTestHelper.Verify(input, AW001);
    }

    [Fact]
    public Task PBI3_2_AW002_ShouldEmitError_WhenMethodIsStatic()
    {
        var input = """
            using AspectWeaver.Abstractions;
            using System;
            public class DIAttribute : AspectAttribute { }

            namespace TestApp
            {
                public static class StaticService
                {
                    [DIAttribute]
                    public static void DoStaticWork() { }
                }

                public class Program
                {
                    public static void Main()
                    {
                        // AW002 should be emitted here.
                        StaticService.DoStaticWork();
                    }
                }
            }
            """;

        // We expect AW002.
        return GeneratorTestHelper.Verify(input, AW002);
    }

    [Fact]
    public Task PBI3_2_AW001_ShouldEmitError_WhenProviderIsInaccessible()
    {
        var input = """
            using AspectWeaver.Abstractions;
            using System;
            public class DIAttribute : AspectAttribute { }

            namespace TestApp
            {
                public class MyService
                {
                    // Inaccessible provider (private)
                    private IServiceProvider ServiceProvider { get; } = null!;

                    [DIAttribute]
                    public void DoWork() { }
                }

                public class Program
                {
                    public static void Main()
                    {
                        var service = new MyService();
                        // AW001 should be emitted here.
                        service.DoWork();
                    }
                }
            }
            """;

        // We expect AW001.
        return GeneratorTestHelper.Verify(input, AW001);
    }
}