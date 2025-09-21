using Microsoft.CodeAnalysis;

namespace AspectWeaver.Generator.Analysis
{
    /// <summary>
    /// Represents a single aspect applied to a method, including its configuration.
    /// </summary>
    /// <param name="AttributeData">The Roslyn representation of the attribute.</param>
    /// <param name="Order">The execution order derived from the AspectAttribute.Order property.</param>
    internal sealed record AspectInfo(
        AttributeData AttributeData,
        int Order
    );
}