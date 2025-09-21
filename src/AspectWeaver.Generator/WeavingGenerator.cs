using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace AspectWeaver.Generator
{
    /// <summary>
    /// The main Source Generator responsible for analyzing the code and weaving aspects
    /// using C# 12 Interceptors. Implements IIncrementalGenerator for optimal performance.
    /// </summary>
    [Generator(LanguageNames.CSharp)]
    public sealed class WeavingGenerator : IIncrementalGenerator
    {
        /// <inheritdoc/>
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // 1. Inject the prerequisite attributes (InterceptsLocationAttribute).
            RegisterPrerequisites(context);

            // 2. The rest of the pipeline (detection and generation) will be defined here in future PBIs.
        }

        private static void RegisterPrerequisites(IncrementalGeneratorInitializationContext context)
        {
            // RegisterPostInitializationOutput executes immediately after parsing.
            context.RegisterPostInitializationOutput(ctx =>
            {
                // Use explicit UTF8 encoding for consistency across build environments.
                var sourceText = SourceText.From(SourceTemplates.InterceptsLocationAttributeSource, Encoding.UTF8);

                // Use a conventional .g.cs extension for generated files.
                ctx.AddSource("InterceptsLocationAttribute.g.cs", sourceText);
            });
        }
    }
}