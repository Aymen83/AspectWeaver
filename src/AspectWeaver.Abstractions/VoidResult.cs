namespace AspectWeaver.Abstractions
{
    /// <summary>
    /// Represents a placeholder type for methods returning void, allowing them
    /// to be handled within the generic IAspectHandler pipeline (ValueTask&lt;TResult&gt;).
    /// </summary>
    // 'readonly struct' ensures minimal overhead.
    public readonly struct VoidResult
    {
        /// <summary>
        /// The default instance of the VoidResult.
        /// </summary>
        public static readonly VoidResult Instance = default;
    }
}