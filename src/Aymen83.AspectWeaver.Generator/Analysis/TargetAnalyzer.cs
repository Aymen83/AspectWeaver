// src/AspectWeaver.Generator/Analysis/TargetAnalyzer.cs
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace Aymen83.AspectWeaver.Generator.Analysis
{
    /// <summary>
    /// Analyzes method symbols to identify applicable aspects and their properties.
    /// </summary>
    internal static class TargetAnalyzer
    {
        // Defines the conventional name for the default order constant.
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
                return [];
            }

            // Sort the aspects based on the Order property (ascending).
            return [.. aspects.OrderBy(a => a.Order)];
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

        /// <summary>
        /// Extracts the 'Order' value from an AspectAttribute, following a specific precedence:
        /// 1. Named argument on the attribute instance (e.g., [MyAspect(Order = 5)]).
        /// 2. A public const int field named 'DefaultOrder' on the attribute class.
        /// 3. A fallback value of 0.
        /// </summary>
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
    }
}