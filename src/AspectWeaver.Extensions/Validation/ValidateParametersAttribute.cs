using System;
using AspectWeaver.Abstractions;

namespace AspectWeaver.Extensions.Validation
{
    /// <summary>
    /// An aspect that automatically validates method parameters before execution based on applied constraints.
    /// Currently supports: <see cref="AspectWeaver.Abstractions.Constraints.NotNullAttribute"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class ValidateParametersAttribute : AspectAttribute
    {
        /// <summary>
        /// The default execution order for this aspect (-1000, ensuring early execution).
        /// </summary>
        public const int DefaultOrder = -1000;

        public ValidateParametersAttribute()
        {
            // Ensure runtime consistency with the compile-time constant.
            Order = DefaultOrder;
        }
    }
}