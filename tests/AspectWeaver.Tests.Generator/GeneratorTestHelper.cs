// tests/AspectWeaver.Tests.Generator/GeneratorTestHelper.cs
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp; // Required for SymbolDisplay
using VerifyXunit;
using AspectWeaver.Generator;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;
using Basic.Reference.Assemblies;
using AspectWeaver.Abstractions;
using System.Text; // Required for StringBuilder

namespace AspectWeaver.Tests.Generator;

public static class GeneratorTestHelper
{
    private static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.CSharp12);
    private static readonly IEnumerable<MetadataReference> References = LoadReferences();

    // Define a constant mock file path for the simulation.
    // Use a realistic path that requires escaping to validate the FormatLiteral fix.
    private const string MockFilePath = @"C:\Users\Aymen83\source\repos\AspectWeaver\tests\SimulatedSource.cs";

    public static Task Verify(string sourceCode)
    {
        // 1. Create the compilation object.
        var compilation = CreateCompilation(sourceCode);

        // (Diagnostic checks 2-6 remain the same...)
        var inputDiagnostics = compilation.GetDiagnostics();
        if (inputDiagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
        {
            throw new InvalidOperationException("Input compilation has errors. Generator results are unreliable.\n" + string.Join("\n", inputDiagnostics));
        }

        var generator = new WeavingGenerator().AsSourceGenerator();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: new[] { generator },
            parseOptions: ParseOptions);

        driver = driver.RunGenerators(compilation);

        var runResult = driver.GetRunResult();
        var generatedTrees = runResult.GeneratedTrees.ToArray();
        var allDiagnostics = runResult.Diagnostics.Concat(generatedTrees.SelectMany(t => t.GetDiagnostics()));

        if (allDiagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
        {
            throw new InvalidOperationException("Generator produced errors or the generated code is invalid:\n" + string.Join("\n", allDiagnostics));
        }

        // 7. Use Verify to assert the results.
        return Verifier.Verify(driver)
            // FIX: Use AddScrubber for compatibility and robustness instead of ScrubInline.
            .AddScrubber(builder => ScrubFilePath(builder, MockFilePath))
            .UseDirectory("Snapshots");
    }

    private static void ScrubFilePath(StringBuilder builder, string filePath)
    {
        // We need to replace the exact string literal used in the generated code.
        // We use FormatLiteral here (with the compatible signature) to ensure we match the generated format.
        var escapedFilePathLiteral = SymbolDisplay.FormatLiteral(filePath, true);

        // Replace the actual path literal (including quotes) with a placeholder literal for portability.
        builder.Replace(escapedFilePathLiteral, "\"[ScrubbedPath]\"");
    }

    private static Compilation CreateCompilation(string source)
    {
        // Provide the MockFilePath when parsing the text.
        var syntaxTree = CSharpSyntaxTree.ParseText(source, ParseOptions, path: MockFilePath);

        // Configure compilation options.
        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            .WithNullableContextOptions(NullableContextOptions.Enable);

        return CSharpCompilation.Create(
            assemblyName: "AspectWeaver.Tests.Simulation",
            syntaxTrees: new[] { syntaxTree },
            references: References,
            options: options);
    }

    // (LoadReferences remains the same, utilizing Basic.Reference.Assemblies)
    private static IEnumerable<MetadataReference> LoadReferences()
    {
        var references = new List<MetadataReference>(Net80.References.All);
        references.Add(MetadataReference.CreateFromFile(typeof(AspectAttribute).Assembly.Location));
        return references;
    }
}