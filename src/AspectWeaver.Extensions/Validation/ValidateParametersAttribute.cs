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
        public ValidateParametersAttribute()
        {
            // Set a low Order value to ensure validation occurs early in the pipeline (before logging, caching, etc.).
            Order = -1000;
        }
    }
}