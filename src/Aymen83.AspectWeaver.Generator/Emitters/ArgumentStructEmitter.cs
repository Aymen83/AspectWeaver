using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;

namespace Aymen83.AspectWeaver.Generator.Emitters
{
    /// <summary>
    /// Generates the specialized ArgumentsStruct for a specific method signature.
    /// </summary>
    internal static class ArgumentStructEmitter
    {
        public const string StructName = "ArgumentsStruct";
        private const string InterfaceName = "global::Aymen83.AspectWeaver.Abstractions.IArgumentsContainer";
        private const string KeyValuePairType = "global::System.Collections.Generic.KeyValuePair<string, object?>";
        private const string ArgumentOutOfRangeExceptionType = "global::System.ArgumentOutOfRangeException";

        public static void Emit(IndentedWriter writer, IMethodSymbol method)
        {
            // Use readonly struct for performance.
            writer.WriteLine($"public readonly struct {StructName} : {InterfaceName}");
            writer.OpenBlock();

            // 1. Fields (Strongly typed, avoids boxing at storage time)
            EmitFields(writer, method);

            // 2. Constructor
            EmitConstructor(writer, method);

            // 3. IArgumentsContainer Implementation
            EmitInterfaceImplementation(writer, method);

            writer.CloseBlock();
        }

        private static void EmitFields(IndentedWriter writer, IMethodSymbol method)
        {
            foreach (var param in method.Parameters)
            {
                var typeFQN = param.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Included).WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier));
                // Fields are private readonly.
                writer.WriteLine($"private readonly {typeFQN} _{param.Name};");
            }
            writer.WriteLine();
        }

        private static void EmitConstructor(IndentedWriter writer, IMethodSymbol method)
        {
            // Generate the constructor parameters (matching the method signature types).
            var parameters = string.Join(", ", method.Parameters.Select(p =>
            {
                var typeFQN = p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Included).WithMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier));
                return $"{typeFQN} {p.Name}";
            }));

            writer.WriteLine($"public {StructName}({parameters})");
            writer.OpenBlock();
            foreach (var param in method.Parameters)
            {
                // Assign parameters to fields.
                writer.WriteLine($"_{param.Name} = {param.Name};");
            }
            writer.CloseBlock();
            writer.WriteLine();
        }

        private static void EmitInterfaceImplementation(IndentedWriter writer, IMethodSymbol method)
        {
            // Count property
            writer.WriteLine($"public int Count => {method.Parameters.Length};");
            writer.WriteLine();

            // Indexer (using switch expression for efficiency)
            writer.WriteLine("public object? this[string parameterName] => parameterName switch");
            writer.OpenBlock();
            foreach (var param in method.Parameters)
            {
                var paramNameLiteral = SymbolDisplay.FormatLiteral(param.Name, true);
                // Note: Boxing occurs here if accessing value types via the interface indexer.
                writer.WriteLine($"{paramNameLiteral} => _{param.Name},");
            }
            // Default case throws exception.
            writer.WriteLine($"_ => throw new {ArgumentOutOfRangeExceptionType}(nameof(parameterName), $\"Parameter '{{parameterName}}' not found.\")");
            writer.CloseBlock(suffix: ";");
            writer.WriteLine();

            // GetEnumerator (Explicit implementation for IEnumerable<T>)
            writer.WriteLine($"public global::System.Collections.Generic.IEnumerator<{KeyValuePairType}> GetEnumerator()");
            writer.OpenBlock();
            if (method.Parameters.Length > 0)
            {
                foreach (var param in method.Parameters)
                {
                    var paramNameLiteral = SymbolDisplay.FormatLiteral(param.Name, true);
                    // Yield return the KeyValuePair. Boxing occurs here for value types.
                    writer.WriteLine($"yield return new {KeyValuePairType}({paramNameLiteral}, _{param.Name});");
                }
            }
            else
            {
                writer.WriteLine("yield break;");
            }
            writer.CloseBlock();
            writer.WriteLine();

            // GetEnumerator (Explicit implementation for non-generic IEnumerable)
            writer.WriteLine("global::System.Collections.IEnumerator global::System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();");
        }
    }
}