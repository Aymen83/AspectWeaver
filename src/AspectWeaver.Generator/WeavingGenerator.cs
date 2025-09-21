using AspectWeaver.Generator.Analysis; // Import Analysis namespace
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Linq; // Required for LINQ extensions
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

            // 2. Define the analysis pipeline

            // Step 2.1: Efficiently get the symbol for AspectAttribute. Runs once per compilation update.
            var aspectAttributeSymbolProvider = context.CompilationProvider.Select((compilation, token) =>
                compilation.GetTypeByMetadataName(AspectAttributeFullName));

            // Step 2.2: Find all invocation sites (Fast Syntax-based filtering).
            var invocationProvider = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: static (node, _) => IsPotentialInterceptionSite(node),
                transform: static (ctx, token) => (InvocationExpressionSyntax)ctx.Node
            );

            // Step 2.3: Combine Invocations, Compilation (for SemanticModel), and the AspectAttribute symbol.
            var combinedProvider = invocationProvider
                .Combine(context.CompilationProvider)
                .Combine(aspectAttributeSymbolProvider);

            // Step 2.4: Perform Semantic Analysis and identify actual targets.
            var interceptionTargets = combinedProvider.Select((pair, token) =>
            {
                var ((invocation, compilation), aspectBaseSymbol) = pair;

                // Safety check: If AspectWeaver.Abstractions is not referenced, the symbol is null.
                if (aspectBaseSymbol == null) return null;

                // Get the SemanticModel for the specific SyntaxTree being analyzed.
                var semanticModel = compilation.GetSemanticModel(invocation.SyntaxTree);

                return AnalyzeInvocation(invocation, aspectBaseSymbol, token, semanticModel);
            })
            .Where(target => target != null)
            .Select((target, _) => target!)
            // CRITICAL: Apply the custom comparer for efficient caching when models contain symbols.
            .WithComparer(InterceptionTargetComparer.Instance);

            // 3. Register the output (Implementation in PBI 2.4+)
            context.RegisterSourceOutput(interceptionTargets.Collect(), (ctx, targets) =>
            {
                // This block will implement the code generation in the next PBI.
                // For now, we leave it empty, but the pipeline is active.
            });
        }

        // (RegisterPrerequisites remains the same)
        private static void RegisterPrerequisites(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput(ctx =>
            {
                var sourceText = SourceText.From(SourceTemplates.InterceptsLocationAttributeSource, Encoding.UTF8);
                ctx.AddSource("InterceptsLocationAttribute.g.cs", sourceText);
            });
        }

        /// <summary>
        /// Fast syntax-based filtering (predicate).
        /// </summary>
        private static bool IsPotentialInterceptionSite(SyntaxNode node)
        {
            // We are looking for method calls.
            return node is InvocationExpressionSyntax;
        }

        /// <summary>
        /// Semantic analysis (transform).
        /// </summary>
        private static InterceptionTarget? AnalyzeInvocation(
            InvocationExpressionSyntax invocation,
            INamedTypeSymbol aspectBaseSymbol,
            CancellationToken token,
            SemanticModel semanticModel)
        {
            token.ThrowIfCancellationRequested();

            // Get the symbol of the method being invoked.
            if (semanticModel.GetSymbolInfo(invocation, token).Symbol is not IMethodSymbol methodSymbol)
            {
                return null;
            }

            // Analyze the method hierarchy to find applied aspects.
            var appliedAspects = TargetAnalyzer.FindApplicableAspects(methodSymbol, aspectBaseSymbol);

            if (appliedAspects.IsEmpty)
            {
                return null;
            }

            // Calculate the precise location of the identifier.
            var location = TargetAnalyzer.CalculateIdentifierLocation(invocation, token);

            return new InterceptionTarget(methodSymbol, location, appliedAspects);
        }
    }
}