using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace AspectWeaver.Generator.Emitters
{
    /// <summary>
    /// Helper record to analyze and store the different components of a method signature required for generating interceptors.
    /// </summary>
    internal sealed record MethodSignature
    {
        // FIX: Define the display format explicitly using the constructor for maximum control and robustness.
        private static readonly SymbolDisplayFormat Format = new SymbolDisplayFormat(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included, // Ensure 'global::' prefix
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeVariance,
            miscellaneousOptions:
                SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                SymbolDisplayMiscellaneousOptions.UseSpecialTypes | // Use 'int' instead of 'System.Int32'
                                                                    // This ensures that types like 'string?' are generated correctly.
                SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
        );

        // Defines the format for parameters, starting from the base format and adding parameter options.
        private static readonly SymbolDisplayFormat ParameterFormat = Format.WithParameterOptions(
                SymbolDisplayParameterOptions.IncludeType |
                SymbolDisplayParameterOptions.IncludeName |
                SymbolDisplayParameterOptions.IncludeModifiers | // ref, out, in, params
                SymbolDisplayParameterOptions.IncludeDefaultValue // Include default values
            );

        public const string InstanceParameterName = "__instance";

        public string ReturnType { get; }
        public string Parameters { get; }
        public string Arguments { get; } // The arguments used when calling the original method
        public bool IsInstanceMethod { get; }

        // Placeholders for PBI 2.7 (Generics)
        public string GenericTypeParameters { get; } = "";
        public string GenericConstraints { get; } = "";

        public MethodSignature(IMethodSymbol method)
        {
            IsInstanceMethod = !method.IsStatic;

            // 1. Return Type
            ReturnType = method.ReturnType.ToDisplayString(Format);

            // 2. Parameters and Arguments
            var parameterList = new List<string>();
            var argumentList = new List<string>();

            // 2.1 Handle 'this' parameter (C# 12 Interceptor requirement for instance methods)
            if (IsInstanceMethod)
            {
                var containingType = method.ContainingType.ToDisplayString(Format);
                parameterList.Add($"this {containingType} {InstanceParameterName}");
            }

            // 2.2 Handle regular parameters
            foreach (var param in method.Parameters)
            {
                // Generate the parameter definition (e.g., "ref int count = 0")
                parameterList.Add(param.ToDisplayString(ParameterFormat));

                // Generate the argument string for the forwarding call (e.g., "ref count")
                string argument = param.Name;
                switch (param.RefKind)
                {
                    case RefKind.Ref:
                        argument = "ref " + argument;
                        break;
                    case RefKind.Out:
                        argument = "out " + argument;
                        break;
                        // 'in' modifier is not required at the call site.
                }
                argumentList.Add(argument);
            }

            Parameters = string.Join(", ", parameterList);
            Arguments = string.Join(", ", argumentList);

            // 3. Generics (PBI 2.7) - Basic support for PoC
            if (method.IsGenericMethod)
            {
                GenericTypeParameters = $"<{string.Join(", ", method.TypeParameters.Select(p => p.Name))}>";
                // Constraints (where T : ...) are complex and deferred.
            }
        }
    }
}