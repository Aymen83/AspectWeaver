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
        /// <summary>
        /// Analyzes a method symbol to find all applicable AspectAttributes, considering inheritance and ensuring uniqueness.
        /// </summary>
        public static ImmutableArray<AspectInfo> FindApplicableAspects(IMethodSymbol methodSymbol, INamedTypeSymbol aspectBaseType)
        {
            var aspects = new List<AspectInfo>();
            // Crucial: Keep track of attribute types already added to handle Inherited=true duplicates.
            // If an aspect is applied on both base and derived types, it should only be included once.
            var addedAttributeTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

            // Helper delegate to process attributes on a symbol
            void ProcessAttributes(ISymbol symbol)
            {
                foreach (var attributeData in symbol.GetAttributes())
                {
                    var attributeClass = attributeData.AttributeClass;
                    if (attributeClass == null) continue;

                    if (IsDerivedFrom(attributeClass, aspectBaseType))
                    {
                        // Ensure uniqueness across the hierarchy.
                        if (addedAttributeTypes.Add(attributeClass))
                        {
                            var order = ExtractOrder(attributeData);
                            aspects.Add(new AspectInfo(attributeData, order));
                        }
                    }
                }
            }

            // 1. Check the method itself and its overrides (base classes).
            var currentMethod = methodSymbol;
            while (currentMethod != null)
            {
                ProcessAttributes(currentMethod);
                currentMethod = currentMethod.OverriddenMethod;
            }

            // 2. Check interface implementations.
            var containingType = methodSymbol.ContainingType;
            foreach (var iface in containingType.AllInterfaces) // AllInterfaces includes inherited interfaces.
            {
                foreach (var interfaceMethod in iface.GetMembers().OfType<IMethodSymbol>())
                {
                    var implementation = containingType.FindImplementationForInterfaceMember(interfaceMethod);
                    if (implementation == null) continue;

                    // Check if the implementation found is the methodSymbol or one of its base overrides.
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

            if (aspects.Count == 0)
            {
                return ImmutableArray<AspectInfo>.Empty;
            }

            // Sort the aspects based on the Order property (ascending).
            return aspects.OrderBy(a => a.Order).ToImmutableArray();
        }

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

        private static int ExtractOrder(AttributeData attributeData)
        {
            // Find the 'Order' property assignment (e.g., [MyAspect(Order = 10)]).
            foreach (var namedArgument in attributeData.NamedArguments)
            {
                if (namedArgument.Key == "Order" && namedArgument.Value.Value is int orderValue)
                {
                    return orderValue;
                }
            }
            return 0; // Default order.
        }

        /// <summary>
        /// Calculates the precise location required for [InterceptsLocation].
        /// It must point to the identifier (the method name) of the invocation.
        /// </summary>
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

            // Good practice to check for cancellation if the token is provided.
            cancellationToken.ThrowIfCancellationRequested();

            // FIX: GetLineSpan() does not accept CancellationToken.
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