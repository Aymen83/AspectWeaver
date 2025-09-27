using Aymen83.AspectWeaver.Generator.Analysis;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aymen83.AspectWeaver.Generator.Emitters
{
    /// <summary>
    /// Represents the signature of a method, providing properties for code generation.
    /// </summary>
    internal sealed record MethodSignature
    {
        // This format is used for Types (return types, parameters, AND constraint types).
        private static readonly SymbolDisplayFormat Format = new(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeVariance,
            miscellaneousOptions:
                SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
                SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
        );

        private static readonly SymbolDisplayFormat ParameterFormat = Format.WithParameterOptions(
               SymbolDisplayParameterOptions.IncludeType |
               SymbolDisplayParameterOptions.IncludeName |
               SymbolDisplayParameterOptions.IncludeModifiers |
               SymbolDisplayParameterOptions.IncludeDefaultValue
           );

        public const string InstanceParameterName = "__instance";
        private const string VoidResultFullName = "global::Aymen83.AspectWeaver.Abstractions.VoidResult";

        public string ReturnType { get; }
        public string Parameters { get; }
        public string Arguments { get; }
        public bool IsInstanceMethod { get; }
        public bool IsAsync { get; }
        public bool ReturnsVoid { get; }
        public string LogicalResultType { get; }

        public string GenericTypeParameters { get; } // e.g., <T1, T2>
        public string GenericConstraints { get; }    // e.g., where T1 : class where T2 : struct

        public MethodSignature(IMethodSymbol method)
        {
            IsInstanceMethod = !method.IsStatic;

            // 1. Parameters and Arguments
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

            // 2. Handle Generics
            if (method.IsGenericMethod)
            {
                // Manually construct the <T1, T2> string.
                GenericTypeParameters = $"<{string.Join(", ", method.TypeParameters.Select(p => p.Name))}>";

                // Extract constraints (where T : ...) using the robust manual approach.
                GenericConstraints = ExtractConstraints(method);
            }
            else
            {
                GenericTypeParameters = "";
                GenericConstraints = "";
            }

            // 3. Analyzing the Return Type (Existing logic)
            ReturnsVoid = method.ReturnsVoid;
            ReturnType = method.ReturnType.ToDisplayString(Format);
            var returnTypeSymbol = method.ReturnType;

            var typeName = returnTypeSymbol.Name;
            IsAsync = typeName == "Task" || typeName == "ValueTask";

            if (ReturnsVoid || (IsAsync && returnTypeSymbol.IsNonGenericTaskOrValueTask()))
            {
                LogicalResultType = VoidResultFullName;
            }
            else if (IsAsync && returnTypeSymbol is INamedTypeSymbol namedType && namedType.IsGenericType)
            {
                LogicalResultType = namedType.TypeArguments[0].ToDisplayString(Format);
            }
            else
            {
                LogicalResultType = ReturnType;
            }
        }

        // Robust constraint extraction based on Aymen83's implementation, with refinements.
        private static string ExtractConstraints(IMethodSymbol method)
        {
            var sb = new StringBuilder();

            foreach (ITypeParameterSymbol typeParameter in method.TypeParameters)
            {
                var constraints = new List<string>();

                // 1. Primary constraints (Order matters according to C# specification)

                if (typeParameter.HasNotNullConstraint)
                {
                    constraints.Add("notnull");
                }

                // Handle 'class'
                if (typeParameter.HasReferenceTypeConstraint)
                {
                    constraints.Add("class");
                }

                // Handle 'struct' and 'unmanaged'
                if (typeParameter.HasUnmanagedTypeConstraint)
                {
                    constraints.Add("unmanaged");
                }
                else if (typeParameter.HasValueTypeConstraint)
                {
                    // 'struct' implies 'new()' implicitly.
                    constraints.Add("struct");
                }

                // 2. Secondary constraints (Base classes, Interfaces)
                foreach (ITypeSymbol constraintType in typeParameter.ConstraintTypes)
                {
                    // CRITICAL: Use the standard 'Format' for fully qualified type names.
                    constraints.Add(constraintType.ToDisplayString(Format));
                }

                // 3. Constructor constraint (new())
                // Only add if 'struct' or 'unmanaged' constraint is not present.
                if (typeParameter.HasConstructorConstraint && !typeParameter.HasValueTypeConstraint && !typeParameter.HasUnmanagedTypeConstraint)
                {
                    constraints.Add("new()");
                }

                if (constraints.Any())
                {
                    // Manually construct the clause: " where T : constraint1, constraint2"
                    // Includes the leading space required by the Emitter.
                    sb.Append($" where {typeParameter.Name} : {string.Join(", ", constraints)}");
                }
            }

            return sb.ToString();
        }
    }
}