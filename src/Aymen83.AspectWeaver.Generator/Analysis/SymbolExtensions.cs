using Microsoft.CodeAnalysis;

namespace Aymen83.AspectWeaver.Generator.Analysis
{
    /// <summary>
    /// Provides extension methods for Roslyn symbols.
    /// </summary>
    internal static class SymbolExtensions
    {
        /// <summary>
        /// Checks if the type symbol represents a non-generic Task or ValueTask.
        /// </summary>
        public static bool IsNonGenericTaskOrValueTask(this ITypeSymbol typeSymbol)
        {
            if (typeSymbol is not INamedTypeSymbol namedType) return false;

            if (namedType.IsGenericType) return false;

            var name = typeSymbol.Name;
            return name == "Task" || name == "ValueTask";
        }
    }
}