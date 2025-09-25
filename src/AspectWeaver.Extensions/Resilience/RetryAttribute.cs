// src/AspectWeaver.Extensions/Resilience/RetryAttribute.cs
using AspectWeaver.Abstractions;

namespace AspectWeaver.Extensions.Resilience
{
    /// <summary>
    /// An aspect that automatically retries the execution of the target method upon failure.
    /// Suitable for handling transient errors (e.g., network issues, deadlocks).
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class RetryAttribute : AspectAttribute
    {
        /// <summary>
        /// The default execution order for this aspect (1000, ensuring late execution/wrapping).
        /// </summary>
        public const int DefaultOrder = 1000;

        /// <summary>
        /// Initializes a new instance of the <see cref="RetryAttribute"/> class.
        /// </summary>
        public RetryAttribute()
        {
            // Ensure runtime consistency with the compile-time constant.
            Order = DefaultOrder;
        }

        /// <summary>
        /// Gets or sets the maximum number of attempts (including the initial call).
        /// Must be greater than or equal to 1. Defaults to 3.
        /// </summary>
        public int MaxAttempts { get; set; } = 3;

        /// <summary>
        /// Gets or sets the delay in milliseconds between attempts (fixed backoff strategy).
        /// Defaults to 100ms.
        /// </summary>
        public int DelayMilliseconds { get; set; } = 100;
    }
}