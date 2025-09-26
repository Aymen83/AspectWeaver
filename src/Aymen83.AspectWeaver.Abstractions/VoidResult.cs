// PBI 5.6: Import System.ComponentModel for EditorBrowsableAttribute.
using System.ComponentModel;

namespace Aymen83.AspectWeaver.Abstractions
{
    /// <summary>
    /// Represents a placeholder type for methods returning void, allowing them
    /// to be handled within the generic IAspectHandler pipeline (ValueTask&lt;TResult&gt;).
    /// </summary>
    // PBI 5.6: Hide this infrastructure type from IntelliSense as it is not intended for direct use.
    [EditorBrowsable(EditorBrowsableState.Never)]
    public readonly struct VoidResult
    {
        /// <summary>
        /// The default instance of the VoidResult.
        /// </summary>
        public static readonly VoidResult Instance = default;
    }
}