using Aymen83.AspectWeaver.Abstractions;
using Aymen83.AspectWeaver.Generator;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Text;
using System.Text.RegularExpressions;

namespace Aymen83.AspectWeaver.Tests.Generator;

public static partial class GeneratorTestHelper
{
    private static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.CSharp12);
    private static readonly IEnumerable<MetadataReference> References = LoadReferences();
    private const string MockFilePath = @"SimulatedSource.cs";

    /// <summary>
    /// Compiles the given source code, runs the <see cref="WeavingGenerator"/>,
    /// and verifies the output using snapshot testing.
    /// </summary>
    /// <param name="sourceCode">The C# source code to compile and generate from.</param>
    /// <param name="expectedDiagnosticIds">An array of diagnostic IDs that are expected to be reported by the generator.</param>
    public static Task Verify(string sourceCode, params string[] expectedDiagnosticIds)
    {
        // Create a Roslyn compilation from the source code.
        var compilation = CreateCompilation(sourceCode);

        // Check for compilation errors in the input source code.
        // We only throw if input errors exist AND we didn't expect any diagnostics from the generator,
        // as input errors can cause cascading failures in the generator.
        var inputDiagnostics = compilation.GetDiagnostics();
        if (expectedDiagnosticIds.Length == 0 && inputDiagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
        {
            throw new InvalidOperationException("Input compilation has errors. Generator results are unreliable.\n" + string.Join("\n", inputDiagnostics));
        }

        // Set up the generator driver.
        var generator = new WeavingGenerator().AsSourceGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [generator],
            parseOptions: ParseOptions);

        // Run the generator and get the results.
        driver = driver.RunGenerators(compilation);
        var runResult = driver.GetRunResult();
        var generatedTrees = runResult.GeneratedTrees.ToArray();

        // Combine diagnostics from both the generator execution and the compilation of generated code.
        var allDiagnostics = runResult.Diagnostics.Concat(generatedTrees.SelectMany(t => t.GetDiagnostics())).ToList();

        // Validate that the reported diagnostics match the expected ones.
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


        // Use Verify to snapshot the generated code and diagnostics.
        // Scrubbers are used to remove machine-specific or run-specific data.
        return Verifier.Verify(driver)
            .AddScrubber(ScrubData)
            .AddScrubber(builder => ScrubFilePath(builder, MockFilePath))
            .UseDirectory("Snapshots");
    }

    /// <summary>
    /// Scrubs the mock file path from the snapshot output to ensure test consistency across different environments.
    /// It replaces both the raw path and the escaped path literal used in the generated code.
    /// </summary>
    private static void ScrubFilePath(StringBuilder builder, string filePath)
    {
        // Scrub the raw path (e.g., in diagnostic messages).
        builder.Replace(filePath, "[ScrubbedPath]");
        // Scrub the escaped literal (e.g., in a C# string literal in generated code).
        var escapedFilePathLiteral = SymbolDisplay.FormatLiteral(filePath, true);
        builder.Replace(escapedFilePathLiteral, "\"[ScrubbedPath]\"");
    }

    /// <summary>
    /// Scrubs the 'data' parameter from the InterceptsLocationAttribute in the snapshot output.
    /// This is necessary because the data is a hash that can change between runs, which would break snapshot tests.
    /// </summary>
    private static void ScrubData(StringBuilder builder)
    {
        string input = builder.ToString();
        string newInput = InterceptsLocationAttributeRegex().Replace(input, "[global::System.Runtime.CompilerServices.InterceptsLocation(version: 1, data: \"[ScrubbedData]\")]");
        builder.Clear();
        builder.Append(newInput);
    }

    /// <summary>
    /// Creates a C# compilation from a source string, configured with the necessary options and references for the generator tests.
    /// </summary>
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

    /// <summary>
    // Loads the required metadata references for the test compilation,
    /// including .NET 8 reference assemblies and the project's own abstractions assembly.
    /// </summary>
    private static List<MetadataReference> LoadReferences()
    {
        var references = new List<MetadataReference>(Net80.References.All)
        {
            // Add a reference to the assembly containing the aspect attributes.
            MetadataReference.CreateFromFile(typeof(AspectAttribute).Assembly.Location)
        };
        return references;
    }

    /// <summary>
    /// A regular expression to find and scrub the 'data' parameter of the InterceptsLocationAttribute.
    /// The 'data' parameter contains a hash that is not stable across builds.
    /// </summary>
    [GeneratedRegex("\\[global::System\\.Runtime\\.CompilerServices\\.InterceptsLocation\\(version: 1, data: \"[a-zA-Z\\d\\+/=]+\"\\)\\]")]
    private static partial Regex InterceptsLocationAttributeRegex();
}