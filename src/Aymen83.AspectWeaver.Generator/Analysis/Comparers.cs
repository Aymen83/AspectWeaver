// src/AspectWeaver.Generator/Analysis/Comparers.cs
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace Aymen83.AspectWeaver.Generator.Analysis
{
    internal sealed class InterceptionTargetComparer : IEqualityComparer<InterceptionTarget>
    {
        public static readonly InterceptionTargetComparer Instance = new();

        public bool Equals(InterceptionTarget? x, InterceptionTarget? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;

            // PBI 3.2: Compare the new property.
            if (x.ProviderAccessExpression != y.ProviderAccessExpression) return false;

            // (Existing comparisons remain)
            if (!x.Location.Equals(y.Location)) return false;
            if (!SymbolEqualityComparer.Default.Equals(x.TargetMethod, y.TargetMethod)) return false;
            return x.AppliedAspects.SequenceEqual(y.AppliedAspects, AspectInfoComparer.Instance);
        }

        public int GetHashCode(InterceptionTarget obj)
        {
            // Manual HashCode combination for .NET Standard 2.0 compatibility.
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + obj.Location.GetHashCode();
                hash = hash * 23 + SymbolEqualityComparer.Default.GetHashCode(obj.TargetMethod);

                // PBI 3.2: Include the new property in the hash.
                hash = hash * 23 + obj.ProviderAccessExpression.GetHashCode();

                foreach (var aspect in obj.AppliedAspects)
                {
                    hash = hash * 23 + AspectInfoComparer.Instance.GetHashCode(aspect);
                }
                return hash;
            }
        }
    }

    // (AspectInfoComparer remains the same)
    internal sealed class AspectInfoComparer : IEqualityComparer<AspectInfo>
    {
        public static readonly AspectInfoComparer Instance = new();

        public bool Equals(AspectInfo? x, AspectInfo? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;

            if (x.Order != y.Order) return false;
            return SymbolEqualityComparer.Default.Equals(x.AttributeData.AttributeClass, y.AttributeData.AttributeClass);
        }

        public int GetHashCode(AspectInfo obj)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + obj.Order.GetHashCode();

                var attributeClassHash = obj.AttributeData.AttributeClass != null
                    ? SymbolEqualityComparer.Default.GetHashCode(obj.AttributeData.AttributeClass)
                    : 0;
                hash = hash * 23 + attributeClassHash;
                return hash;
            }
        }
    }
}