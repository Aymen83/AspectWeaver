using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace Aymen83.AspectWeaver.Generator.Analysis
{
    /// <summary>
    /// Defines equality comparers for the analysis types.
    /// </summary>
    internal sealed class InterceptionTargetComparer : IEqualityComparer<InterceptionTarget>
    {
        public static readonly InterceptionTargetComparer Instance = new();

        public bool Equals(InterceptionTarget? x, InterceptionTarget? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;

            if (x.ProviderAccessExpression != y.ProviderAccessExpression) return false;

            if (!x.Location.Equals(y.Location)) return false;
            if (!SymbolEqualityComparer.Default.Equals(x.TargetMethod, y.TargetMethod)) return false;
            return x.AppliedAspects.SequenceEqual(y.AppliedAspects, AspectInfoComparer.Instance);
        }

        public int GetHashCode(InterceptionTarget obj)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + obj.Location.GetHashCode();
                hash = hash * 23 + SymbolEqualityComparer.Default.GetHashCode(obj.TargetMethod);
                hash = hash * 23 + obj.ProviderAccessExpression.GetHashCode();

                foreach (var aspect in obj.AppliedAspects)
                {
                    hash = hash * 23 + AspectInfoComparer.Instance.GetHashCode(aspect);
                }
                return hash;
            }
        }
    }

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