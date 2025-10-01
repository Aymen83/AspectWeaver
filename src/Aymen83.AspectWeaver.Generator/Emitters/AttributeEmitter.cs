// src/AspectWeaver.Generator/Emitters/AttributeEmitter.cs
using Microsoft.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;

namespace Aymen83.AspectWeaver.Generator.Emitters
{
    /// <summary>
    /// Handles the generation of C# code required to instantiate (rehydrate) attributes from AttributeData.
    /// </summary>
    internal static class AttributeEmitter
    {
        /// <summary>
        /// Generates the full C# expression string to instantiate the attribute.
        /// Example: new global::MyAspectAttribute(10) { Order = 5 }
        /// </summary>
        public static string GenerateAttributeInstantiation(AttributeData attributeData)
        {
            var attributeType = attributeData.AttributeClass!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Included));

            var constructorArgs = string.Join(", ", attributeData.ConstructorArguments.Select(arg => TypedConstantToString(arg)));
            var namedArgs = string.Join(", ", attributeData.NamedArguments.Select(kvp => $"{kvp.Key} = {TypedConstantToString(kvp.Value)}"));

            var instantiation = $"new {attributeType}({constructorArgs})";

            if (!string.IsNullOrEmpty(namedArgs))
            {
                // Append object initializer syntax.
                instantiation += $" {{ {namedArgs} }}";
            }

            return instantiation;
        }


        // This logic is moved directly from the previous PipelineEmitter implementation.
        private static string TypedConstantToString(TypedConstant constant)
        {
            if (constant.IsNull) return "null";

            if (constant.Kind == TypedConstantKind.Primitive)
            {
                if (constant.Value is string s)
                {
                    return SymbolDisplay.FormatLiteral(s, true);
                }
                if (constant.Value is bool b) return b ? "true" : "false";
                if (constant.Value is char c)
                {
                    return SymbolDisplay.FormatLiteral(c, true);
                }

                // Handle numeric types using InvariantCulture for robustness in the generated code.
                return global::System.Convert.ToString(constant.Value, global::System.Globalization.CultureInfo.InvariantCulture);
            }

            if (constant.Kind == TypedConstantKind.Enum)
            {
                var enumType = constant.Type!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Included));
                // Cast the underlying numeric value to the enum type.
                return $"({enumType})({constant.Value})";
            }

            if (constant.Kind == TypedConstantKind.Type && constant.Value is ITypeSymbol typeSymbol)
            {
                // Generate typeof(T) expression.
                return $"typeof({typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Included))})";
            }

            // Fallback for complex types or arrays (not fully supported in this MVP but necessary for robustness).
            return "default";
        }
    }
}