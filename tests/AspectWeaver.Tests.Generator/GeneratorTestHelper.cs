using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using AspectWeaver.Abstractions;
using System.Reflection;
using VerifyXunit;
using AspectWeaver.Generator;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AspectWeaver.Tests.Generator;

public static class GeneratorTestHelper
{
    // Configure C# 12 language version explicitly for the simulation.
    private static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.CSharp12);

    public static Task Verify(string sourceCode)
    {
        // 1. Create the compilation object from the source code.
        var compilation = CreateCompilation(sourceCode);

        // 2. Instantiate the generator.
        var generator = new WeavingGenerator().AsSourceGenerator();

        // 3. Create the driver, ensuring it uses the correct parse options.
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: new[] { generator },
            parseOptions: ParseOptions);

        // 4. Run the generator on the compilation.
        driver = driver.RunGenerators(compilation);

        // 5. Use Verify to assert the results.
        return Verifier.Verify(driver)
            .UseDirectory("Snapshots"); // Organize snapshots in a dedicated folder
    }

    private static Compilation CreateCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, ParseOptions);

        // Get references to necessary assemblies for the simulation.
        var references = new List<MetadataReference>
        {
            // CoreLib (System.Private.CoreLib)
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),

            // Explicitly reference System.Runtime (crucial for many basic types in modern .NET)
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),

            // AspectWeaver.Abstractions
            MetadataReference.CreateFromFile(typeof(AspectAttribute).Assembly.Location),

            // System.Threading.Tasks.Extensions (for ValueTask, required by Abstractions)
             MetadataReference.CreateFromFile(typeof(ValueTask).Assembly.Location)
        };

        // Configure compilation options.
        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            .WithNullableContextOptions(NullableContextOptions.Enable); // Match project settings

        return CSharpCompilation.Create(
            assemblyName: "AspectWeaver.Tests.Simulation",
            syntaxTrees: new[] { syntaxTree },
            references: references,
            options: options);
    }
}