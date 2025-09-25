using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;
using AspectWeaver.Generator.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AspectWeaver.Generator.Analyzers
{
    /// <summary>
    /// Analyzes the configuration values of the RetryAttribute.
    /// Reports AW005.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RetryAttributeAnalyzer : DiagnosticAnalyzer
    {
        // We must use the FQN of the attribute we are analyzing.
        private const string RetryAttributeFullName = "AspectWeaver.Extensions.Resilience.RetryAttribute";

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(DiagnosticDescriptors.AW005_InvalidAttributeConfiguration);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // Register the analysis action to run when an attribute is applied to a method.
            context.RegisterSymbolAction(AnalyzeAttributeApplication, SymbolKind.Method);
        }

        private void AnalyzeAttributeApplication(SymbolAnalysisContext context)
        {
            var methodSymbol = (IMethodSymbol)context.Symbol;
            var compilation = context.Compilation;

            var retryAttributeType = compilation.GetTypeByMetadataName(RetryAttributeFullName);
            if (retryAttributeType == null) return;

            // Find the specific application of the RetryAttribute on this method.
            var attributeData = methodSymbol.GetAttributes()
                .FirstOrDefault(ad => SymbolEqualityComparer.Default.Equals(ad.AttributeClass, retryAttributeType));

            if (attributeData == null) return;

            // Analyze MaxAttempts configuration.
            AnalyzeMaxAttempts(attributeData, context);
        }

        private void AnalyzeMaxAttempts(AttributeData attributeData, SymbolAnalysisContext context)
        {
            // Find the 'MaxAttempts' named argument.
            var maxAttemptsArg = attributeData.NamedArguments.FirstOrDefault(kvp => kvp.Key == "MaxAttempts");

            // If MaxAttempts was not explicitly set, the default value is used, which is valid (3).
            if (maxAttemptsArg.Key == null) return;

            // Check if the value is a valid integer.
            if (maxAttemptsArg.Value.Value is int maxAttemptsValue)
            {
                // Validate the range: Must be >= 1.
                if (maxAttemptsValue < 1)
                {
                    // Determine the location of the invalid value in the source code for precise reporting.
                    var location = GetNamedArgumentLocation(attributeData, "MaxAttempts", context);

                    var diagnostic = Diagnostic.Create(
                        descriptor: DiagnosticDescriptors.AW005_InvalidAttributeConfiguration,
                        location: location,
                        // Message arguments: Attribute Name, Reason.
                        messageArgs: new object[] { attributeData.AttributeClass!.Name, "MaxAttempts must be greater than or equal to 1." }
                    );
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        // Helper to find the precise location of a named argument's value (e.g., the '0' in MaxAttempts = 0).
        private static Location? GetNamedArgumentLocation(AttributeData attributeData, string argumentName, SymbolAnalysisContext context)
        {
            var syntaxReference = attributeData.ApplicationSyntaxReference;
            if (syntaxReference == null) return null;

            // We must cast the syntax node to the specific C# type (AttributeSyntax).
            if (syntaxReference.GetSyntax(context.CancellationToken) is AttributeSyntax attributeSyntax)
            {
                var argumentSyntax = attributeSyntax.ArgumentList?.Arguments
                    .OfType<AttributeArgumentSyntax>()
                    .FirstOrDefault(arg => arg.NameEquals?.Name.Identifier.Text == argumentName);

                // Return the location of the expression (the value assigned).
                return argumentSyntax?.Expression.GetLocation();
            }
            return null;
        }
    }
}