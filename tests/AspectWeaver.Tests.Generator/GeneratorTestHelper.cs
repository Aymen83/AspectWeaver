// tests/AspectWeaver.Tests.Generator/GeneratorTestHelper.cs
using AspectWeaver.Abstractions;
using AspectWeaver.Generator;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Text;

namespace AspectWeaver.Tests.Generator;

public static class GeneratorTestHelper
{
    // ... (Constants and Properties remain the same)
    private static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.CSharp12);
    private static readonly IEnumerable<MetadataReference> References = LoadReferences();
    private const string MockFilePath = @"C:\Users\Aymen83\source\repos\AspectWeaver\tests\SimulatedSource.cs";


    // PBI 3.2: Update Verify signature to accept expected diagnostic IDs.
    public static Task Verify(string sourceCode, params string[] expectedDiagnosticIds)
    {
        // 1. Create the compilation object.
        var compilation = CreateCompilation(sourceCode);

        // 2. Check for input diagnostics.
        var inputDiagnostics = compilation.GetDiagnostics();
        // We only throw if input errors exist AND we didn't expect any diagnostics from the generator (as input errors might cascade).
        if (expectedDiagnosticIds.Length == 0 && inputDiagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
        {
            throw new InvalidOperationException("Input compilation has errors. Generator results are unreliable.\n" + string.Join("\n", inputDiagnostics));
        }

        // (Generator instantiation and driver creation remains the same...)
        var generator = new WeavingGenerator().AsSourceGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [generator],
            parseOptions: ParseOptions);

        // 5. Run the generator.
        driver = driver.RunGenerators(compilation);

        // 6. Check for diagnostics produced during or after generation.
        var runResult = driver.GetRunResult();
        var generatedTrees = runResult.GeneratedTrees.ToArray();
        // Combine all diagnostics (Generator execution + Generated code compilation).
        var allDiagnostics = runResult.Diagnostics.Concat(generatedTrees.SelectMany(t => t.GetDiagnostics())).ToList();

        // PBI 3.2: Validate expected diagnostics.
        var actualDiagnosticIds = allDiagnostics.Select(d => d.Id).ToHashSet();

        if (expectedDiagnosticIds.Length > 0)
        {
            // Ensure all expected diagnostics were reported.
            if (!expectedDiagnosticIds.All(id => actualDiagnosticIds.Contains(id)))
            {
                throw new InvalidOperationException($"Expected diagnostics [{string.Join(", ", expectedDiagnosticIds)}] not fully found. Actual diagnostics:\n" + string.Join("\n", allDiagnostics));
            }

            // Ensure no unexpected errors occurred.
            var unexpectedErrors = allDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Error && !expectedDiagnosticIds.Contains(d.Id)).ToList();
            if (unexpectedErrors.Count != 0)
            {
                throw new InvalidOperationException("Generator produced unexpected errors:\n" + string.Join("\n", unexpectedErrors));
            }
        }
        else
        {
            // If no diagnostics were expected, ensure no errors occurred.
            if (allDiagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
            {
                throw new InvalidOperationException("Generator produced errors or the generated code is invalid:\n" + string.Join("\n", allDiagnostics));
            }
        }


        // 7. Use Verify to assert the results (Snapshots the generated code).
        return Verifier.Verify(driver)
            .AddScrubber(builder => ScrubFilePath(builder, MockFilePath))
            .UseDirectory("Snapshots");
    }

    // ... (ScrubFilePath, CreateCompilation, LoadReferences remain the same)
    private static void ScrubFilePath(StringBuilder builder, string filePath)
    {
        // Scrub the raw path (in case Verify snapshots diagnostic messages)
        builder.Replace(filePath, "[ScrubbedPath]");
        // Scrub the escaped literal (for generated code)
        var escapedFilePathLiteral = SymbolDisplay.FormatLiteral(filePath, true);
        builder.Replace(escapedFilePathLiteral, "\"[ScrubbedPath]\"");
    }

    private static CSharpCompilation CreateCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, ParseOptions, path: MockFilePath);

        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            .WithNullableContextOptions(NullableContextOptions.Enable);

        return CSharpCompilation.Create(
            assemblyName: "AspectWeaver.Tests.Simulation",
            syntaxTrees: [syntaxTree],
            references: References,
            options: options);
    }

    private static List<MetadataReference> LoadReferences()
    {
        var references = new List<MetadataReference>(Net80.References.All)
        {
            MetadataReference.CreateFromFile(typeof(AspectAttribute).Assembly.Location)
        };
        return references;
    }
}