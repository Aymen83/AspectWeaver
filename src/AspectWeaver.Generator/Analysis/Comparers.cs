using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace AspectWeaver.Generator.Analysis
{
    /// <summary>
    /// Custom comparer for <see cref="InterceptionTarget"/> to ensure efficient caching.
    /// </summary>
    internal sealed class InterceptionTargetComparer : IEqualityComparer<InterceptionTarget>
    {
        public static readonly InterceptionTargetComparer Instance = new();

        public bool Equals(InterceptionTarget? x, InterceptionTarget? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;

            // 1. Compare Location (Value equality)
            if (!x.Location.Equals(y.Location)) return false;

            // 2. Compare TargetMethod using Roslyn's SymbolEqualityComparer
            if (!SymbolEqualityComparer.Default.Equals(x.TargetMethod, y.TargetMethod)) return false;

            // 3. Compare AppliedAspects sequences
            return x.AppliedAspects.SequenceEqual(y.AppliedAspects, AspectInfoComparer.Instance);
        }

        public int GetHashCode(InterceptionTarget obj)
        {
            // FIX: Manual HashCode combination for .NET Standard 2.0 compatibility.
            // System.HashCode is not available.
            unchecked // Overflow is fine, just wrap
            {
                int hash = 17; // Prime seed
                hash = hash * 23 + obj.Location.GetHashCode();
                // Use the SymbolEqualityComparer for the symbol hash code.
                hash = hash * 23 + SymbolEqualityComparer.Default.GetHashCode(obj.TargetMethod);

                // Combine hashes for the aspects sequence (order matters)
                foreach (var aspect in obj.AppliedAspects)
                {
                    hash = hash * 23 + AspectInfoComparer.Instance.GetHashCode(aspect);
                }
                return hash;
            }
        }
    }

    /// <summary>
    /// Helper comparer for AspectInfo.
    /// </summary>
    internal sealed class AspectInfoComparer : IEqualityComparer<AspectInfo>
    {
        public static readonly AspectInfoComparer Instance = new();

        public bool Equals(AspectInfo? x, AspectInfo? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;

            // For caching, we compare the order and the attribute type (using the AttributeClass symbol).
            if (x.Order != y.Order) return false;
            return SymbolEqualityComparer.Default.Equals(x.AttributeData.AttributeClass, y.AttributeData.AttributeClass);
        }

        public int GetHashCode(AspectInfo obj)
        {
            // FIX: Manual HashCode combination for .NET Standard 2.0 compatibility.
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + obj.Order.GetHashCode();

                // AttributeClass might be null in edge cases during compilation, so we handle it defensively.
                var attributeClassHash = obj.AttributeData.AttributeClass != null
                    ? SymbolEqualityComparer.Default.GetHashCode(obj.AttributeData.AttributeClass)
                    : 0;
                hash = hash * 23 + attributeClassHash;
                return hash;
            }
        }
    }
}