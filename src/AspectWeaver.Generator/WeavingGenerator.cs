using AspectWeaver.Generator.Analysis;
using AspectWeaver.Generator.Emitters;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
// Import Diagnostics namespace
using AspectWeaver.Generator.Diagnostics;

namespace AspectWeaver.Generator
{
    [Generator(LanguageNames.CSharp)]
    public sealed class WeavingGenerator : IIncrementalGenerator
    {
        private const string AspectAttributeFullName = "AspectWeaver.Abstractions.AspectAttribute";
        private const string IServiceProviderFullName = "System.IServiceProvider";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // 1. Inject prerequisites
            RegisterPrerequisites(context);

            // 2. Define the analysis pipeline

            // Step 2.1: Get Compilation and essential symbols.
            var compilationAndSymbolsProvider = context.CompilationProvider.Select((compilation, token) =>
            {
                return (
                    Compilation: compilation,
                    AspectBaseSymbol: compilation.GetTypeByMetadataName(AspectAttributeFullName),
                    ServiceProviderSymbol: compilation.GetTypeByMetadataName(IServiceProviderFullName)
                );
            });

            // Step 2.2: Find all invocation sites and capture the SemanticModel efficiently.
            var invocationProvider = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: static (node, _) => node is InvocationExpressionSyntax,
                // Efficiently capture the node and its SemanticModel during the syntax phase.
                transform: static (ctx, token) => (Invocation: (InvocationExpressionSyntax)ctx.Node, Model: ctx.SemanticModel)
            );

            // Step 2.3: Combine Invocations and Compilation/Symbols.
            var combinedProvider = invocationProvider.Combine(compilationAndSymbolsProvider);

            // Step 2.4: Perform Semantic Analysis (Incremental).
            // This produces a stream of (Target, Diagnostic) tuples.
            var analysisResults = combinedProvider.Select((pair, token) =>
            {
                // Destructure the combined input efficiently.
                var ((invocationSyntax, semanticModel), symbols) = pair;

                // Safety check: If AspectWeaver.Abstractions is not referenced.
                if (symbols.AspectBaseSymbol == null) return (Target: (InterceptionTarget?)null, Diagnostic: (Diagnostic?)null);

                // Call the analysis method with all required inputs.
                return AnalyzeInvocation(invocationSyntax, symbols.AspectBaseSymbol, symbols.ServiceProviderSymbol, symbols.Compilation, token, semanticModel);
            });


            // Step 2.5: Report Diagnostics.
            var diagnostics = analysisResults
                .Select((r, _) => r.Diagnostic)
                .Where(d => d != null)
                .Select((d, _) => d!);

            context.RegisterSourceOutput(diagnostics, (ctx, diagnostic) =>
            {
                ctx.ReportDiagnostic(diagnostic);
            });


            // Step 2.6: Filter valid targets and prepare for generation.
            var interceptionTargets = analysisResults
                .Select((r, _) => r.Target)
                .Where(target => target != null)
                .Select((target, _) => target!)
                // CRITICAL: Apply the custom comparer for efficient caching.
                .WithComparer(InterceptionTargetComparer.Instance);


            // 3. Register the output (Generation Phase)
            context.RegisterSourceOutput(interceptionTargets.Collect(), ExecuteGeneration);
        }

        // (ExecuteGeneration remains the same)
        private static void ExecuteGeneration(SourceProductionContext context, ImmutableArray<InterceptionTarget> targets)
        {
            if (targets.IsDefaultOrEmpty) return;

            var generatedSource = InterceptorEmitter.Emit(targets);

            if (!string.IsNullOrEmpty(generatedSource))
            {
                context.AddSource("AspectWeaver.Interceptors.g.cs", SourceText.From(generatedSource, Encoding.UTF8));
            }
        }

        private static void RegisterPrerequisites(IncrementalGeneratorInitializationContext context)
        {
            context.RegisterPostInitializationOutput(ctx =>
            {
                // Inject InterceptsLocationAttribute
                var sourceText1 = SourceText.From(SourceTemplates.InterceptsLocationAttributeSource, Encoding.UTF8);
                ctx.AddSource("InterceptsLocationAttribute.g.cs", sourceText1);

                // PBI 3.3: Stop injecting PlaceholderServiceProvider
                // The injection code previously here is removed.
            });
        }

        /// <summary>
        /// Semantic analysis (transform). Updated to return both the target and potential diagnostics.
        /// </summary>
        private static (InterceptionTarget? Target, Diagnostic? Diagnostic) AnalyzeInvocation(
            InvocationExpressionSyntax invocation,
            INamedTypeSymbol aspectBaseSymbol,
            INamedTypeSymbol? serviceProviderSymbol,
            Compilation compilation, // Required for accessibility checks
            CancellationToken token,
            SemanticModel semanticModel)
        {
            token.ThrowIfCancellationRequested();

            // 1. Get the symbol of the method being invoked.
            if (semanticModel.GetSymbolInfo(invocation, token).Symbol is not IMethodSymbol methodSymbol)
            {
                return (null, null);
            }

            // 2. Analyze the method hierarchy to find applied aspects.
            var appliedAspects = TargetAnalyzer.FindApplicableAspects(methodSymbol, aspectBaseSymbol);

            if (appliedAspects.IsEmpty)
            {
                return (null, null);
            }

            // 3. PBI 3.2: Analyze IServiceProvider availability.

            // Get the precise Location object for diagnostic reporting.
            var diagnosticLocation = GetIdentifierLocation(invocation);

            // 3.1 Handle Static Method Diagnostic (AW002).
            if (methodSymbol.IsStatic)
            {
                var diagnostic = Diagnostic.Create(
                   descriptor: DiagnosticDescriptors.AW002_StaticMethodNotSupported,
                   location: diagnosticLocation,
                   messageArgs: new object[] { methodSymbol.Name }
               );
                return (null, diagnostic);
            }


            // 3.2 Discover Provider Access Expression.
            // Pass the compilation object for robust accessibility checks.
            var providerAccessExpression = ServiceProviderAnalyzer.FindServiceProviderAccess(methodSymbol, serviceProviderSymbol, compilation);

            // 3.3 Handle Not Found Diagnostic (AW001).
            if (providerAccessExpression == null)
            {
                var diagnostic = Diagnostic.Create(
                    descriptor: DiagnosticDescriptors.AW001_ServiceProviderNotFound,
                    location: diagnosticLocation,
                    // Message arguments: Method Name, Containing Type Name.
                    messageArgs: new object[] { methodSymbol.Name, methodSymbol.ContainingType.Name }
                );
                // Return the diagnostic and null target (abort generation for this site).
                return (null, diagnostic);
            }

            // 4. Calculate the precise location for the interceptor attribute.
            var locationInfo = TargetAnalyzer.CalculateIdentifierLocation(invocation, token);

            // 5. Success: Create the valid InterceptionTarget.
            var target = new InterceptionTarget(methodSymbol, locationInfo, appliedAspects, providerAccessExpression);
            return (target, null);
        }

        // Helper to get the Location object for diagnostic reporting.
        private static Location? GetIdentifierLocation(InvocationExpressionSyntax invocation)
        {
            SyntaxNode identifierNode = invocation.Expression switch
            {
                MemberAccessExpressionSyntax memberAccess => memberAccess.Name,
                MemberBindingExpressionSyntax memberBinding => memberBinding.Name,
                _ => invocation.Expression
            };
            return identifierNode.GetLocation();
        }
    }
}