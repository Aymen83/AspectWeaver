using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using Aymen83.AspectWeaver.Generator.Diagnostics;

namespace Aymen83.AspectWeaver.Generator.Analyzers
{
    /// <summary>
    /// Analyzes the application targets of AspectAttributes to ensure they are only used on methods.
    /// Reports AW003.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AspectTargetAnalyzer : DiagnosticAnalyzer
    {
        private const string AspectAttributeFullName = "Aymen83.AspectWeaver.Abstractions.AspectAttribute";

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = 
            [DiagnosticDescriptors.AW003_InvalidAspectTarget];

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            // Register the analysis action to run on the completion of the compilation.
            context.RegisterCompilationAction(AnalyzeCompilation);
        }

        private void AnalyzeCompilation(CompilationAnalysisContext context)
        {
            var compilation = context.Compilation;
            var aspectBaseType = compilation.GetTypeByMetadataName(AspectAttributeFullName);

            if (aspectBaseType == null) return;

            // Iterate through all symbols in the compilation.
            AnalyzeNamespace(compilation.GlobalNamespace, aspectBaseType, context);
        }

        // Helper to recursively traverse namespaces and types.
        private void AnalyzeNamespace(INamespaceSymbol namespaceSymbol, INamedTypeSymbol aspectBaseType, CompilationAnalysisContext context)
        {
            foreach (var type in namespaceSymbol.GetTypeMembers())
            {
                AnalyzeType(type, aspectBaseType, context);
            }

            foreach (var nestedNamespace in namespaceSymbol.GetNamespaceMembers())
            {
                AnalyzeNamespace(nestedNamespace, aspectBaseType, context);
            }
        }

        // Helper to analyze members within a type.
        private void AnalyzeType(INamedTypeSymbol typeSymbol, INamedTypeSymbol aspectBaseType, CompilationAnalysisContext context)
        {
            // Check members of the type.
            foreach (var member in typeSymbol.GetMembers())
            {
                // We only care about members that are NOT methods.
                if (member.Kind != SymbolKind.Method)
                {
                    AnalyzeMemberAttributes(member, aspectBaseType, context);
                }
            }

            // Check nested types recursively.
            foreach (var nestedType in typeSymbol.GetTypeMembers())
            {
                AnalyzeType(nestedType, aspectBaseType, context);
            }
        }

        private void AnalyzeMemberAttributes(ISymbol member, INamedTypeSymbol aspectBaseType, CompilationAnalysisContext context)
        {
            foreach (var attributeData in member.GetAttributes())
            {
                var attributeClass = attributeData.AttributeClass;
                if (attributeClass == null) continue;

                // Check if the attribute derives from AspectAttribute.
                if (IsDerivedFrom(attributeClass, aspectBaseType))
                {
                    // Found an AspectAttribute applied to a non-method target. Report AW003.
                    var diagnostic = Diagnostic.Create(
                        descriptor: DiagnosticDescriptors.AW003_InvalidAspectTarget,
                        // Report the diagnostic at the location of the attribute usage.
                        location: attributeData.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation(),
                        // Message arguments: Attribute Name, Target Kind (e.g., Property, Field).
                        messageArgs: [attributeClass.Name, member.Kind.ToString()]
                    );
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        // Helper for inheritance check.
        private static bool IsDerivedFrom(INamedTypeSymbol? type, INamedTypeSymbol baseType)
        {
            var current = type;
            while (current != null)
            {
                if (SymbolEqualityComparer.Default.Equals(current, baseType)) return true;
                current = current.BaseType;
            }
            return false;
        }
    }
}