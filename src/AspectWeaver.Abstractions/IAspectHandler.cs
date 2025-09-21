using System;
using System.Threading.Tasks; // Required for ValueTask

namespace AspectWeaver.Abstractions
{
    /// <summary>
    /// Defines the contract for an aspect handler responsible for implementing the interception logic.
    /// Handlers are typically resolved from the Dependency Injection container and managed according to their configured lifecycle (e.g., Singleton, Scoped).
    /// </summary>
    /// <typeparam name="TAttribute">The type of the <see cref="AspectAttribute"/> this handler implements the logic for.</typeparam>
    public interface IAspectHandler<TAttribute> where TAttribute : AspectAttribute
    {
        /// <summary>
        /// Implements the "Around" interception logic.
        /// This method wraps the execution of the next handler in the chain or the target method itself.
        /// </summary>
        /// <remarks>
        /// This unified signature supports both synchronous and asynchronous methods efficiently using <see cref="ValueTask{TResult}"/>.
        ///
        /// To continue the execution flow, the implementation MUST invoke the <paramref name="next"/> delegate.
        /// Failure to invoke <paramref name="next"/> will short-circuit the execution.
        ///
        /// Example implementation (e.g., Exception Logging):
        /// <code>
        /// public async ValueTask&lt;TResult&gt; InterceptAsync&lt;TResult&gt;(TAttribute attribute, InvocationContext context, Func&lt;InvocationContext, ValueTask&lt;TResult&gt;&gt; next)
        /// {
        ///     try
        ///     {
        ///         // Before logic (optional)
        ///         var result = await next(context).ConfigureAwait(false);
        ///         // After logic (optional)
        ///         return result;
        ///     }
        ///     catch (Exception ex)
        ///     {
        ///         // Exception handling logic
        ///         _logger.LogError(ex, "Method {MethodName} failed.", context.MethodName);
        ///         throw;
        ///     }
        /// }
        /// </code>
        /// </remarks>
        /// <typeparam name="TResult">The type of the return value of the intercepted method.</typeparam>
        /// <param name="attribute">The instance of the attribute applied to the method, containing configuration data.</param>
        /// <param name="context">The context of the current invocation.</param>
        /// <param name="next">A delegate representing the next action in the interception chain (the "Proceed" action).</param>
        /// <returns>A <see cref="ValueTask{TResult}"/> representing the result of the invocation.</returns>
        ValueTask<TResult> InterceptAsync<TResult>(
            TAttribute attribute,
            InvocationContext context,
            Func<InvocationContext, ValueTask<TResult>> next);
    }
}