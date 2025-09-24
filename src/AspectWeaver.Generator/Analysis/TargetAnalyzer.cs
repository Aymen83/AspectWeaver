// src/AspectWeaver.Generator/Analysis/TargetAnalyzer.cs
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace AspectWeaver.Generator.Analysis
{
    internal static class TargetAnalyzer
    {
        // PBI 5.2 FIX: Define the conventional name for the default order constant.
        private const string DefaultOrderFieldName = "DefaultOrder";

        /// <summary>
        /// Analyzes a method symbol to find all applicable AspectAttributes, considering inheritance and ensuring uniqueness.
        /// </summary>
        public static ImmutableArray<AspectInfo> FindApplicableAspects(IMethodSymbol methodSymbol, INamedTypeSymbol aspectBaseType)
        {
            var aspects = new List<AspectInfo>();
            var addedAttributeTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

            void ProcessAttributes(ISymbol symbol)
            {
                foreach (var attributeData in symbol.GetAttributes())
                {
                    var attributeClass = attributeData.AttributeClass;
                    if (attributeClass == null) continue;

                    if (IsDerivedFrom(attributeClass, aspectBaseType))
                    {
                        if (addedAttributeTypes.Add(attributeClass))
                        {
                            // Call the updated ExtractOrder method.
                            var order = ExtractOrder(attributeData);
                            aspects.Add(new AspectInfo(attributeData, order));
                        }
                    }
                }
            }

            // (Hierarchy traversal logic remains the same...)
            // 1. Check the method itself and its overrides (base classes).
            var currentMethod = methodSymbol;
            while (currentMethod != null)
            {
                ProcessAttributes(currentMethod);
                currentMethod = currentMethod.OverriddenMethod;
            }

            // 2. Check interface implementations.
            var containingType = methodSymbol.ContainingType;
            // Ensure containingType is not null before accessing AllInterfaces
            if (containingType != null)
            {
                foreach (var iface in containingType.AllInterfaces)
                {
                    foreach (var interfaceMethod in iface.GetMembers().OfType<IMethodSymbol>())
                    {
                        var implementation = containingType.FindImplementationForInterfaceMember(interfaceMethod);
                        if (implementation == null) continue;

                        bool isRelated = false;
                        var temp = methodSymbol;
                        while (temp != null)
                        {
                            if (SymbolEqualityComparer.Default.Equals(temp, implementation))
                            {
                                isRelated = true;
                                break;
                            }
                            temp = temp.OverriddenMethod;
                        }

                        if (isRelated)
                        {
                            ProcessAttributes(interfaceMethod);
                        }
                    }
                }
            }

            if (aspects.Count == 0)
            {
                return ImmutableArray<AspectInfo>.Empty;
            }

            // Sort the aspects based on the Order property (ascending).
            return aspects.OrderBy(a => a.Order).ToImmutableArray();
        }

        // (IsDerivedFrom remains the same)
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

        // PBI 5.2 FIX: Robustly extract the Order value.
        private static int ExtractOrder(AttributeData attributeData)
        {
            // 1. Check Named Arguments (Explicit override at usage site: [MyAspect(Order = 5)])
            foreach (var namedArgument in attributeData.NamedArguments)
            {
                if (namedArgument.Key == "Order" && namedArgument.Value.Value is int orderValue)
                {
                    return orderValue;
                }
            }

            // 2. Check for DefaultOrder constant on the attribute class (Defined default).
            // This works even if the attribute is in a referenced assembly (metadata).
            var attributeClass = attributeData.AttributeClass;
            if (attributeClass != null)
            {
                // Look for the constant field by the conventional name.
                var defaultOrderField = attributeClass.GetMembers(DefaultOrderFieldName)
                                                      .OfType<IFieldSymbol>()
                                                      .FirstOrDefault();

                // Ensure it exists, is constant, and has a value of type int.
                if (defaultOrderField != null && defaultOrderField.IsConst && defaultOrderField.HasConstantValue)
                {
                    if (defaultOrderField.ConstantValue is int defaultOrderValue)
                    {
                        return defaultOrderValue;
                    }
                }
            }

            // 3. Fallback to absolute default if no explicit order or constant is defined.
            return 0;
        }

        // (CalculateIdentifierLocation remains the same)
        public static InterceptionLocation CalculateIdentifierLocation(InvocationExpressionSyntax invocation, CancellationToken cancellationToken)
        {
            // Determine the syntax node representing the identifier being called.
            SyntaxNode identifierNode = invocation.Expression switch
            {
                // e.g., myObject.MyMethod() or StaticClass.MyMethod()
                MemberAccessExpressionSyntax memberAccess => memberAccess.Name,

                // e.g., myObject?.MyMethod() (Conditional Access)
                MemberBindingExpressionSyntax memberBinding => memberBinding.Name,

                // Handles local calls, static calls, and generic calls (IdentifierNameSyntax or GenericNameSyntax)
                _ => invocation.Expression
            };

            cancellationToken.ThrowIfCancellationRequested();

            var lineSpan = identifierNode.GetLocation().GetLineSpan();
            var position = lineSpan.StartLinePosition;

            // C# Interceptors require 1-based indexing. Roslyn provides 0-based indexing.
            return new InterceptionLocation(
                FilePath: lineSpan.Path,
                Line: position.Line + 1,
                Character: position.Character + 1
            );
        }
    }
}