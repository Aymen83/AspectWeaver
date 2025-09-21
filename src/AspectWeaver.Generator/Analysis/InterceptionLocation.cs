namespace AspectWeaver.Generator.Analysis
{
    /// <summary>
    /// Represents the precise location in the source code where an invocation occurs.
    /// Used for the [InterceptsLocation] attribute.
    /// </summary>
    // 'readonly record struct' provides efficient immutability and value-based equality.
    internal readonly record struct InterceptionLocation(
        string FilePath,
        int Line, // 1-based index
        int Character // 1-based index
    );
}