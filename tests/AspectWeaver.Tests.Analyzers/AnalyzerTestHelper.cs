using AspectWeaver.Abstractions;
using AspectWeaver.Extensions.Resilience; // Required to get Extensions assembly reference
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using System.Collections.Immutable;

namespace AspectWeaver.Tests.Analyzers;

// Define a type alias for the specific test configuration (CSharpAnalyzerTest with XUnit verifier).
public class AnalyzerTest<TAnalyzer> : CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
    where TAnalyzer : Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer, new()
{
    public AnalyzerTest(string sourceCode)
    {
        TestCode = sourceCode;

        // Configure the environment: .NET 8, C# 12, and robust references.
        ReferenceAssemblies = new ReferenceAssemblies(
            "net8.0",
            new PackageIdentity("Microsoft.NETCore.App.Ref", "8.0.0"),
            Path.Combine("ref", "net8.0"))
            // Use Basic.Reference.Assemblies for robustness.
            .AddAssemblies([.. Net80.References.All.Select(r => r.FilePath!)]);

        // Add project-specific references (Abstractions and Extensions).
        TestState.AdditionalReferences.Add(typeof(AspectAttribute).Assembly);
        TestState.AdditionalReferences.Add(typeof(RetryAttribute).Assembly);

        // Ensure C# 12 is used for parsing.
        SolutionTransforms.Add((solution, projectId) =>
        {
            return solution.WithProjectParseOptions(projectId, new CSharpParseOptions(
                languageVersion: LanguageVersion.CSharp12));
        });
    }
}

public static class AnalyzerTestHelper
{
    /// <summary>
    /// Helper method to run an analyzer test and verify diagnostics.
    /// </summary>
    public static async Task VerifyAnalyzerAsync<TAnalyzer>(string sourceCode, params DiagnosticResult[] expectedDiagnostics)
        where TAnalyzer : Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer, new()
    {
        var test = new AnalyzerTest<TAnalyzer>(sourceCode);
        test.ExpectedDiagnostics.AddRange(expectedDiagnostics);
        await test.RunAsync();
    }
}