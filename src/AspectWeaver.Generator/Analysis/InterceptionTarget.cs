using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace AspectWeaver.Generator.Analysis
{
    /// <summary>
    /// Represents a specific call site that needs to be intercepted.
    /// </summary>
    /// <param name="TargetMethod">The symbol of the method being invoked.</param>
    /// <param name="Location">The location of the invocation in the source code.</param>
    /// <param name="AppliedAspects">The list of aspects applied to the method, sorted by execution order.</param>
    internal sealed record InterceptionTarget(
        IMethodSymbol TargetMethod,
        InterceptionLocation Location,
        ImmutableArray<AspectInfo> AppliedAspects
    );
}