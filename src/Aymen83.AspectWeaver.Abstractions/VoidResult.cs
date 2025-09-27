using System.ComponentModel;

namespace Aymen83.AspectWeaver.Abstractions
{
    /// <summary>
    /// Represents a placeholder type for methods returning void, allowing them
    /// to be handled within the generic IAspectHandler pipeline (ValueTask&lt;TResult&gt;).
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public readonly struct VoidResult
    {
        /// <summary>
        /// The default instance of the VoidResult.
        /// </summary>
        public static readonly VoidResult Instance = default;
    }
}