using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Aymen83.AspectWeaver.Generator.Analysis
{
    /// <summary>
    /// Represents a specific call site that needs to be intercepted.
    /// </summary>
    internal sealed record InterceptionTarget(
        IMethodSymbol TargetMethod,
        InterceptionLocation Location,
        ImmutableArray<AspectInfo> AppliedAspects,
        string ProviderAccessExpression
    );
}