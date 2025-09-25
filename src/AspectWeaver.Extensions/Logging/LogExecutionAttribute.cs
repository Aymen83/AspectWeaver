// src/AspectWeaver.Extensions/Logging/LogExecutionAttribute.cs
using System;
using AspectWeaver.Abstractions;
using Microsoft.Extensions.Logging;

namespace AspectWeaver.Extensions.Logging
{
    /// <summary>
    /// An aspect that automatically logs the execution flow of the target method,
    /// including start time, completion time, duration, and exceptions.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public sealed class LogExecutionAttribute : AspectAttribute
    {
        /// <summary>
        /// The default execution order for this aspect (100).
        /// </summary>
        public const int DefaultOrder = 100;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogExecutionAttribute"/> class.
        /// </summary>
        public LogExecutionAttribute()
        {
            // Ensure runtime consistency with the compile-time constant.
            Order = DefaultOrder;
        }

        /// <summary>
        /// Gets or sets the <see cref="LogLevel"/> used for standard execution messages (Start, End, Duration).
        /// Defaults to <see cref="LogLevel.Information"/>.
        /// </summary>
        public LogLevel Level { get; set; } = LogLevel.Information;

        /// <summary>
        /// Gets or sets the <see cref="LogLevel"/> used when an exception occurs.
        /// Defaults to <see cref="LogLevel.Error"/>.
        /// </summary>
        public LogLevel ExceptionLevel { get; set; } = LogLevel.Error;

        /// <summary>
        /// Gets or sets a value indicating whether the arguments passed to the method should be included in the entry log.
        /// Defaults to false.
        /// </summary>
        /// <remarks>
        /// Warning: Enabling this might log sensitive information. Uses structured logging.
        /// </remarks>
        public bool LogArguments { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether the return value of the method should be included in the exit log.
        /// Defaults to false.
        /// </summary>
        /// <remarks>
        /// Warning: Enabling this might log sensitive information. Uses structured logging.
        /// </remarks>
        public bool LogReturnValue { get; set; } = false;
    }
}