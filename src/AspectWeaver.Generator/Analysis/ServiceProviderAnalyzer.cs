using AspectWeaver.Generator.Emitters; // Required for MethodSignature.InstanceParameterName
using Microsoft.CodeAnalysis;
using System.Linq;

namespace AspectWeaver.Generator.Analysis
{
    internal static class ServiceProviderAnalyzer
    {
        // Define the prioritized list of conventional names (PBI 3.1).
        private static readonly string[] ConventionalNames = {
            "ServiceProvider",
            "_serviceProvider",
            "Services"
        };

        /// <summary>
        /// Analyzes the containing type to find an accessible IServiceProvider member.
        /// </summary>
        /// <param name="methodSymbol">The method being intercepted.</param>
        /// <param name="serviceProviderSymbol">The symbol for System.IServiceProvider.</param>
        /// <param name="compilation">The current compilation context (required for accessibility checks).</param>
        /// <returns>The C# expression to access the provider, or null if not found.</returns>
        public static string? FindServiceProviderAccess(IMethodSymbol methodSymbol, INamedTypeSymbol? serviceProviderSymbol, Compilation compilation)
        {
            // 1. Check prerequisites.
            if (serviceProviderSymbol == null) return null;

            // 2. Static methods are handled by the caller, but we ensure safety here.
            if (methodSymbol.IsStatic)
            {
                return null;
            }

            var containingType = methodSymbol.ContainingType;
            if (containingType == null) return null;

            // 3. Search for the member in the type hierarchy.
            string? memberName = FindAccessibleMember(containingType, serviceProviderSymbol, compilation);

            if (memberName == null)
            {
                return null;
            }

            // 4. Construct the access expression (e.g., "__instance.ServiceProvider").
            return $"{MethodSignature.InstanceParameterName}.{memberName}";
        }

        private static string? FindAccessibleMember(INamedTypeSymbol type, INamedTypeSymbol targetTypeSymbol, Compilation compilation)
        {
            // Traverse the type hierarchy.
            var currentType = type;
            while (currentType != null)
            {
                // Get all fields and properties (with getters) of the correct type.
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

                // Filter by accessibility: Must be accessible from the generated interceptor.
                var accessibleMembers = potentialMembers
                    .Where(m => IsAccessible(m, compilation))
                    .ToList();

                if (accessibleMembers.Count == 0)
                {
                    currentType = currentType.BaseType;
                    continue;
                }

                // Apply prioritization logic (PBI 3.1 conventions).
                foreach (var conventionalName in ConventionalNames)
                {
                    var match = accessibleMembers.FirstOrDefault(m => m.Name == conventionalName);
                    if (match != null)
                    {
                        return match.Name;
                    }
                }

                // If no conventional name matched, return the first accessible member found.
                return accessibleMembers.First().Name;
            }

            return null;
        }

        // Robust accessibility check.
        private static bool IsAccessible(ISymbol member, Compilation compilation)
        {
            // Public and Internal are always accessible within the same compilation context.
            if (member.DeclaredAccessibility == Accessibility.Public ||
                member.DeclaredAccessibility == Accessibility.Internal)
            {
                return true;
            }

            // ProtectedOrInternal requires checking assembly access (handles InternalsVisibleTo scenarios).
            if (member.DeclaredAccessibility == Accessibility.ProtectedOrInternal)
            {
                return compilation.Assembly.GivesAccessTo(member.ContainingAssembly);
            }

            // Private and Protected are not accessible.
            return false;
        }
    }
}