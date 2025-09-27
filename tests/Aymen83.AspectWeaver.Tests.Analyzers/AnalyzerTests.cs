using Aymen83.AspectWeaver.Generator.Analyzers;
using Aymen83.AspectWeaver.Generator.Diagnostics;
using AspectVerifier = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<Aymen83.AspectWeaver.Generator.Analyzers.AspectTargetAnalyzer, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;
using RetryVerifier = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<Aymen83.AspectWeaver.Generator.Analyzers.RetryAttributeAnalyzer, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace Aymen83.AspectWeaver.Tests.Analyzers;

public class AnalyzerTests
{
    #region AW003 Tests (AspectTargetAnalyzer)

    [Fact]
    public async Task AW003_ShouldNotTrigger_OnMethod()
    {
        var testCode = """
            using Aymen83.AspectWeaver.Abstractions;
            public class MyAspectAttribute : AspectAttribute { }

            public class TestClass
            {
                [MyAspect] // Valid target
                public void MyMethod() { }
            }
            """ ;

        // Expect no diagnostics.
        await AnalyzerTestHelper.VerifyAnalyzerAsync<AspectTargetAnalyzer>(testCode);
    }

    [Fact]
    public async Task AW003_ShouldTrigger_OnProperty()
    {
        var testCode = """
            using System;
            using Aymen83.AspectWeaver.Abstractions;
            [AttributeUsage(AttributeTargets.Property)]
            public class MyAspectAttribute : AspectAttribute { }

            public class TestClass
            {
                [MyAspect] // Invalid target
                public int MyProperty { get; set; }
            }
            """ ;

        // Expect AW003 at the specific location (Line 8, Column 6).
        var expected = AspectVerifier.Diagnostic(DiagnosticDescriptors.AW003_InvalidAspectTarget)
            .WithLocation(8, 6)
            .WithArguments("MyAspectAttribute", "Property");

        await AnalyzerTestHelper.VerifyAnalyzerAsync<AspectTargetAnalyzer>(testCode, expected);
    }

    [Fact]
    public async Task AW003_ShouldTrigger_OnField()
    {
        var testCode = """
            using System;
            using Aymen83.AspectWeaver.Abstractions;

            [AttributeUsage(AttributeTargets.Field)]
            public class MyAspectAttribute : AspectAttribute { }

            public class TestClass
            {
                [MyAspect] // Invalid target
                public string _myField;
            }
            """ ;

        var expected = AspectVerifier.Diagnostic(DiagnosticDescriptors.AW003_InvalidAspectTarget)
            .WithLocation(9, 6)
            .WithArguments("MyAspectAttribute", "Field");

        await AnalyzerTestHelper.VerifyAnalyzerAsync<AspectTargetAnalyzer>(testCode, expected);
    }

    #endregion

    #region AW005 Tests (RetryAttributeAnalyzer)

    [Fact]
    public async Task AW005_ShouldNotTrigger_WhenConfigurationIsValid()
    {
        var testCode = """
            using Aymen83.AspectWeaver.Extensions.Resilience;

            public class TestService
            {
                [Retry(MaxAttempts = 1)] // Valid (Minimum allowed)
                public void Operation1() { }

                [Retry] // Valid (Default value)
                public void Operation2() { }
            }
            """ ;

        // Expect no diagnostics.
        await AnalyzerTestHelper.VerifyAnalyzerAsync<RetryAttributeAnalyzer>(testCode);
    }

    [Fact]
    public async Task AW005_ShouldTrigger_WhenMaxAttemptsIsZero()
    {
        var testCode = """
            using Aymen83.AspectWeaver.Extensions.Resilience;

            public class TestService
            {
                [Retry(MaxAttempts = 0)] // Invalid
                public void Operation() { }
            }
            """ ;

        // Expect AW005 at the location of the invalid value (Line 5, Column 26).
        var expected = RetryVerifier.Diagnostic(DiagnosticDescriptors.AW005_InvalidAttributeConfiguration)
            .WithLocation(5, 26)
            .WithArguments("RetryAttribute", "MaxAttempts must be greater than or equal to 1.");

        await AnalyzerTestHelper.VerifyAnalyzerAsync<RetryAttributeAnalyzer>(testCode, expected);
    }

    [Fact]
    public async Task AW005_ShouldTrigger_WhenMaxAttemptsIsNegative()
    {
        var testCode = """
            using Aymen83.AspectWeaver.Extensions.Resilience;

            public class TestService
            {
                [Retry(MaxAttempts = -5)] // Invalid
                public void Operation() { }
            }
            """ ;

        // Expect AW005 at the location of the invalid value (Line 5, 26).
        var expected = RetryVerifier.Diagnostic(DiagnosticDescriptors.AW005_InvalidAttributeConfiguration)
            .WithLocation(5, 26)
            .WithArguments("RetryAttribute", "MaxAttempts must be greater than or equal to 1.");

        await AnalyzerTestHelper.VerifyAnalyzerAsync<RetryAttributeAnalyzer>(testCode, expected);
    }

    #endregion
}