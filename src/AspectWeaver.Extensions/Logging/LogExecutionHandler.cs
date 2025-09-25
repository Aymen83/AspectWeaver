// src/AspectWeaver.Extensions/Logging/LogExecutionHandler.cs
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using AspectWeaver.Abstractions;
using Microsoft.Extensions.Logging;

namespace AspectWeaver.Extensions.Logging
{
    /// <summary>
    /// Handler for <see cref="LogExecutionAttribute"/>.
    /// Resolves <see cref="ILoggerFactory"/> via DI and logs execution details.
    /// </summary>
    public sealed class LogExecutionHandler : IAspectHandler<LogExecutionAttribute>
    {
        private readonly ILoggerFactory _loggerFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogExecutionHandler"/> class.
        /// </summary>
        /// <param name="loggerFactory">The logger factory used to create loggers.</param>
        public LogExecutionHandler(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        /// <inheritdoc />
        public async ValueTask<TResult> InterceptAsync<TResult>(LogExecutionAttribute attribute, InvocationContext context, Func<InvocationContext, ValueTask<TResult>> next)
        {
            // Create a logger specific to the target type.
            var logger = _loggerFactory.CreateLogger(context.TargetTypeName);

            // Optimization: Check if either the standard level or the exception level is enabled.
            if (!logger.IsEnabled(attribute.Level) && !logger.IsEnabled(attribute.ExceptionLevel))
            {
                // If logging is entirely disabled for these levels, execute the method directly.
                return await next(context).ConfigureAwait(false);
            }

            // 1. Log Entry
            if (logger.IsEnabled(attribute.Level))
            {
                LogEntry(logger, attribute, context);
            }

            // 2. Execute and measure time
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = await next(context).ConfigureAwait(false);
                stopwatch.Stop();

                // 3. Log Success
                if (logger.IsEnabled(attribute.Level))
                {
                    LogExit(logger, attribute, context, stopwatch.Elapsed, result);
                }
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                // 4. Log Exception
                if (logger.IsEnabled(attribute.ExceptionLevel))
                {
                    LogException(logger, attribute, context, stopwatch.Elapsed, ex);
                }
                throw;
            }
        }

        // (Private helper methods LogEntry, LogExit, LogException remain the same - they do not require XML docs)
        private static void LogEntry(ILogger logger, LogExecutionAttribute attribute, InvocationContext context)
        {
            if (attribute.LogArguments)
            {
                logger.Log(
                    attribute.Level,
                    "Executing method {MethodName} with arguments {@Arguments}",
                    context.MethodName,
                    context.Arguments);
            }
            else
            {
                logger.Log(
                    attribute.Level,
                    "Executing method {MethodName}",
                    context.MethodName);
            }
        }

        private static void LogExit(ILogger logger, LogExecutionAttribute attribute, InvocationContext context, TimeSpan duration, object? result)
        {
            var durationMs = duration.TotalMilliseconds;

            if (attribute.LogReturnValue)
            {
                logger.Log(
                   attribute.Level,
                   "Method {MethodName} completed in {DurationMs}ms with result {@ReturnValue}",
                   context.MethodName,
                   durationMs,
                   result);
            }
            else
            {
                logger.Log(
                    attribute.Level,
                    "Method {MethodName} completed in {DurationMs}ms",
                    context.MethodName,
                    durationMs);
            }
        }

        private static void LogException(ILogger logger, LogExecutionAttribute attribute, InvocationContext context, TimeSpan duration, Exception ex)
        {
            var durationMs = duration.TotalMilliseconds;

            logger.Log(
               attribute.ExceptionLevel,
               ex,
               "Method {MethodName} failed after {DurationMs}ms",
               context.MethodName,
               durationMs);
        }
    }
}