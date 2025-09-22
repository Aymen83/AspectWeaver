using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using AspectWeaver.Generator.Analysis; // Import SymbolExtensions

namespace AspectWeaver.Generator.Emitters
{
    internal sealed record MethodSignature
    {
        // (Keep the Format configuration that works in your environment, e.g., using IncludeNullableReferenceTypeModifier)
        private static readonly SymbolDisplayFormat Format = new SymbolDisplayFormat(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeVariance,
            miscellaneousOptions:
                SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
                // Use the flag that works in your environment:
                SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
        );

        private static readonly SymbolDisplayFormat ParameterFormat = Format.WithParameterOptions(
               SymbolDisplayParameterOptions.IncludeType |
               SymbolDisplayParameterOptions.IncludeName |
               SymbolDisplayParameterOptions.IncludeModifiers |
               SymbolDisplayParameterOptions.IncludeDefaultValue
           );

        public const string InstanceParameterName = "__instance";
        private const string VoidResultFullName = "global::AspectWeaver.Abstractions.VoidResult";

        public string ReturnType { get; } // e.g., void, int, Task<int>
        public string Parameters { get; }
        public string Arguments { get; }
        public bool IsInstanceMethod { get; }

        // New Properties for PBI 2.5/2.6
        public bool IsAsync { get; }
        public bool ReturnsVoid { get; }
        public string LogicalResultType { get; } // The TResult for the pipeline (e.g., int or VoidResult)


        // Placeholders for PBI 2.7 (Generics)
        public string GenericTypeParameters { get; } = "";
        public string GenericConstraints { get; } = "";

        public MethodSignature(IMethodSymbol method)
        {
            IsInstanceMethod = !method.IsStatic;

            // 1. Parameters and Arguments (Existing logic)
            var parameterList = new List<string>();
            var argumentList = new List<string>();
            if (IsInstanceMethod)
            {
                var containingType = method.ContainingType.ToDisplayString(Format);
                parameterList.Add($"this {containingType} {InstanceParameterName}");
            }
            foreach (var param in method.Parameters)
            {
                parameterList.Add(param.ToDisplayString(ParameterFormat));
                string argument = param.Name;
                switch (param.RefKind)
                {
                    case RefKind.Ref: argument = "ref " + argument; break;
                    case RefKind.Out: argument = "out " + argument; break;
                }
                argumentList.Add(argument);
            }
            Parameters = string.Join(", ", parameterList);
            Arguments = string.Join(", ", argumentList);

            // 2. Generics (Existing logic)
            if (method.IsGenericMethod)
            {
                GenericTypeParameters = $"<{string.Join(", ", method.TypeParameters.Select(p => p.Name))}>";
            }

            // 3. New Logic: Analyzing the Return Type
            ReturnsVoid = method.ReturnsVoid;
            ReturnType = method.ReturnType.ToDisplayString(Format);
            var returnTypeSymbol = method.ReturnType;

            // Basic Async Detection (Check if the return type name is Task or ValueTask).
            var typeName = returnTypeSymbol.Name;
            IsAsync = typeName == "Task" || typeName == "ValueTask";

            // Determine LogicalResultType (TResult)
            if (ReturnsVoid || (IsAsync && returnTypeSymbol.IsNonGenericTaskOrValueTask()))
            {
                // void, Task, or ValueTask (non-generic)
                LogicalResultType = VoidResultFullName;
            }
            else if (IsAsync && returnTypeSymbol is INamedTypeSymbol namedType && namedType.IsGenericType)
            {
                // Task<T> or ValueTask<T>. Extract T.
                LogicalResultType = namedType.TypeArguments[0].ToDisplayString(Format);
            }
            else
            {
                // Synchronous non-void return type.
                LogicalResultType = ReturnType;
            }
        }
    }
}