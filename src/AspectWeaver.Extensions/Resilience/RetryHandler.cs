// src/AspectWeaver.Extensions/Resilience/RetryHandler.cs
using AspectWeaver.Abstractions;
using System;
using System.Threading.Tasks;

namespace AspectWeaver.Extensions.Resilience
{
    /// <summary>
    /// Handler for <see cref="RetryAttribute"/>.
    /// Implements a fixed-delay retry policy.
    /// </summary>
    public sealed class RetryHandler : IAspectHandler<RetryAttribute>
    {
        // This handler does not require DI for its core logic.

        /// <inheritdoc />
        public async ValueTask<TResult> InterceptAsync<TResult>(RetryAttribute attribute, InvocationContext context, Func<InvocationContext, ValueTask<TResult>> next)
        {
            // Validate configuration defensively.
            int attemptsLeft = attribute.MaxAttempts;
            if (attemptsLeft < 1)
            {
                // If misconfigured, default to a single attempt (no retry).
                attemptsLeft = 1;
            }

            int delayMs = attribute.DelayMilliseconds;
            if (delayMs < 0)
            {
                delayMs = 0;
            }

            while (true)
            {
                try
                {
                    // Attempt the execution (calls the next aspect in the chain or the target method).
                    return await next(context).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // Handle the failure.
                    attemptsLeft--;

                    // Check if attempts are exhausted.
                    if (attemptsLeft <= 0)
                    {
                        // Rethrow the exception if no more attempts are left.
                        throw;
                    }

                    // Wait before the next attempt (non-blocking delay).
                    if (delayMs > 0)
                    {
                        // We use Task.Delay for asynchronous waiting.
                        await Task.Delay(delayMs).ConfigureAwait(false);
                    }
                    // The loop continues for the next attempt.
                }
            }
        }
    }
}