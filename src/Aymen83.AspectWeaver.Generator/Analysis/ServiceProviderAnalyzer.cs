using Aymen83.AspectWeaver.Generator.Emitters;
using Microsoft.CodeAnalysis;
using System.Linq;

namespace Aymen83.AspectWeaver.Generator.Analysis
{
    /// <summary>
    /// Analyzes the containing type to find an accessible IServiceProvider member.
    /// </summary>
    internal static class ServiceProviderAnalyzer
    {
        private static readonly string[] ConventionalNames = [
            "ServiceProvider",
            "_serviceProvider",
            "Services"
        ];

        /// <summary>
        /// Analyzes the containing type to find an accessible IServiceProvider member.
        /// </summary>
        /// <param name="methodSymbol">The method being intercepted.</param>
        /// <param name="serviceProviderSymbol">The symbol for System.IServiceProvider.</param>
        /// <param name="compilation">The current compilation context (required for accessibility checks).</param>
        /// <returns>The C# expression to access the provider, or null if not found.</returns>
        public static string? FindServiceProviderAccess(IMethodSymbol methodSymbol, INamedTypeSymbol? serviceProviderSymbol, Compilation compilation)
        {
            if (serviceProviderSymbol == null) return null;

            if (methodSymbol.IsStatic)
            {
                return null;
            }

            var containingType = methodSymbol.ContainingType;
            if (containingType == null) return null;

            string? memberName = FindAccessibleMember(containingType, serviceProviderSymbol, compilation);

            if (memberName == null)
            {
                return null;
            }

            return $"{MethodSignature.InstanceParameterName}.{memberName}";
        }

        private static string? FindAccessibleMember(INamedTypeSymbol type, INamedTypeSymbol targetTypeSymbol, Compilation compilation)
        {
            var currentType = type;
            while (currentType != null)
            {
                var potentialMembers = currentType.GetMembers()
                    .Where(m => m.Kind == SymbolKind.Field || (m.Kind == SymbolKind.Property && ((IPropertySymbol)m).GetMethod != null))
                    .Where(m =>
                    {
                        var memberType = m.Kind == SymbolKind.Field ? ((IFieldSymbol)m).Type : ((IPropertySymbol)m).Type;
                        return SymbolEqualityComparer.Default.Equals(memberType, targetTypeSymbol);
                    })
                    .ToList();

                if (potentialMembers.Count == 0)
                {
                    currentType = currentType.BaseType;
                    continue;
                }

                var accessibleMembers = potentialMembers
                    .Where(m => IsAccessible(m, compilation))
                    .ToList();

                if (accessibleMembers.Count == 0)
                {
                    currentType = currentType.BaseType;
                    continue;
                }

                foreach (var conventionalName in ConventionalNames)
                {
                    var match = accessibleMembers.FirstOrDefault(m => m.Name == conventionalName);
                    if (match != null)
                    {
                        return match.Name;
                    }
                }

                return accessibleMembers.First().Name;
            }

            return null;
        }

        private static bool IsAccessible(ISymbol member, Compilation compilation)
        {
            if (member.DeclaredAccessibility == Accessibility.Public ||
                member.DeclaredAccessibility == Accessibility.Internal)
            {
                return true;
            }

            if (member.DeclaredAccessibility == Accessibility.ProtectedOrInternal)
            {
                return compilation.Assembly.GivesAccessTo(member.ContainingAssembly);
            }

            return false;
        }
    }
}