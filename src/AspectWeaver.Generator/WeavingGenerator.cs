using AspectWeaver.Generator.Analysis;
using AspectWeaver.Generator.Emitters; // Import Emitters
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable; // Import ImmutableArray
using System.Linq;
using System.Text;
using System.Threading;

namespace AspectWeaver.Generator
{
    [Generator(LanguageNames.CSharp)]
    public sealed class WeavingGenerator : IIncrementalGenerator
    {
        private const string AspectAttributeFullName = "AspectWeaver.Abstractions.AspectAttribute";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // 1. Inject prerequisites
            RegisterPrerequisites(context);

            // 2. Define the analysis pipeline (Steps 2.1 to 2.3 remain the same)

            var aspectAttributeSymbolProvider = context.CompilationProvider.Select((compilation, token) =>
                compilation.GetTypeByMetadataName(AspectAttributeFullName));

            var invocationProvider = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: static (node, _) => IsPotentialInterceptionSite(node),
                transform: static (ctx, token) => (InvocationExpressionSyntax)ctx.Node
            );

            var combinedProvider = invocationProvider
                .Combine(context.CompilationProvider)
                .Combine(aspectAttributeSymbolProvider);

            // Step 2.4: Perform Semantic Analysis and identify actual targets.
            var interceptionTargets = combinedProvider.Select((pair, token) =>
            {
                var ((invocation, compilation), aspectBaseSymbol) = pair;

                if (aspectBaseSymbol == null) return null;

                var semanticModel = compilation.GetSemanticModel(invocation.SyntaxTree);

                return AnalyzeInvocation(invocation, aspectBaseSymbol, token, semanticModel);
            })
            .Where(target => target != null)
            .Select((target, _) => target!)
            .WithComparer(InterceptionTargetComparer.Instance);

            // 3. Register the output (Generation Phase)
            // Collect all targets found across the compilation and pass them to the execution method.
            context.RegisterSourceOutput(interceptionTargets.Collect(), ExecuteGeneration);
        }

        /// <summary>
        /// Executes the code generation phase based on the collected analysis results.
        /// </summary>
        private static void ExecuteGeneration(SourceProductionContext context, ImmutableArray<InterceptionTarget> targets)
        {
            // Optimization: If no targets were found, do nothing.
            if (targets.IsDefaultOrEmpty) return;

            // Use the Emitter to generate the C# source code.
            var generatedSource = InterceptorEmitter.Emit(targets);

            if (!string.IsNullOrEmpty(generatedSource))
            {
                // Add the generated source file to the compilation output.
                context.AddSource("AspectWeaver.Interceptors.g.cs", SourceText.From(generatedSource, Encoding.UTF8));
            }
        }

        // (RegisterPrerequisites, IsPotentialInterceptionSite, AnalyzeInvocation methods remain the same)
        private static void RegisterPrerequisites(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput(ctx =>
            {
                var sourceText = SourceText.From(SourceTemplates.InterceptsLocationAttributeSource, Encoding.UTF8);
                ctx.AddSource("InterceptsLocationAttribute.g.cs", sourceText);
            });
        }

        private static bool IsPotentialInterceptionSite(SyntaxNode node)
        {
            return node is InvocationExpressionSyntax;
        }

        private static InterceptionTarget? AnalyzeInvocation(
            InvocationExpressionSyntax invocation,
            INamedTypeSymbol aspectBaseSymbol,
            CancellationToken token,
            SemanticModel semanticModel)
        {
            token.ThrowIfCancellationRequested();

            if (semanticModel.GetSymbolInfo(invocation, token).Symbol is not IMethodSymbol methodSymbol)
            {
                return null;
            }

            var appliedAspects = TargetAnalyzer.FindApplicableAspects(methodSymbol, aspectBaseSymbol);

            if (appliedAspects.IsEmpty)
            {
                return null;
            }

            var location = TargetAnalyzer.CalculateIdentifierLocation(invocation, token);

            return new InterceptionTarget(methodSymbol, location, appliedAspects);
        }
    }
}