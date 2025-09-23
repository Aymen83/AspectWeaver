using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace AspectWeaver.Generator.Analysis
{
    /// <summary>
    /// Represents a specific call site that needs to be intercepted.
    /// </summary>
    internal sealed record InterceptionTarget(
        IMethodSymbol TargetMethod,
        InterceptionLocation Location,
        ImmutableArray<AspectInfo> AppliedAspects,
        // PBI 3.2: The C# expression used to access the IServiceProvider (e.g., "__instance.ServiceProvider").
        string ProviderAccessExpression
    );
}