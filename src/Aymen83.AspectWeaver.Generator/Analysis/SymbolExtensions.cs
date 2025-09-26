using Microsoft.CodeAnalysis;

namespace Aymen83.AspectWeaver.Generator.Analysis
{
    internal static class SymbolExtensions
    {
        /// <summary>
        /// Checks if the type symbol represents a non-generic Task or ValueTask.
        /// </summary>
        public static bool IsNonGenericTaskOrValueTask(this ITypeSymbol typeSymbol)
        {
            if (typeSymbol is not INamedTypeSymbol namedType) return false;
            // If it's generic (like Task<T>), it's not a non-generic task.
            if (namedType.IsGenericType) return false;

            var name = typeSymbol.Name;
            // Basic check by name.
            return name == "Task" || name == "ValueTask";
        }
    }
}