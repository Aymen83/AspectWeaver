using System;
using System.Threading.Tasks;

namespace Aymen83.AspectWeaver.Abstractions
{
    /// <summary>
    /// Defines the interface for a handler that implements the logic of a specific aspect.
    /// Handlers are typically resolved from the Dependency Injection container.
    /// </summary>
    /// <typeparam name="TAttribute">The type of the <see cref="AspectAttribute"/> this handler corresponds to.</typeparam>
    public interface IAspectHandler<TAttribute> where TAttribute : AspectAttribute
    {
        /// <summary>
        /// The core interception method implementing the "Around" advice pattern.
        /// </summary>
        /// <typeparam name="TResult">The logical result type of the intercepted method (use <see cref="VoidResult"/> for void methods).</typeparam>
        /// <param name="attribute">The instance of the attribute applied to the method, containing configuration values.</param>
        /// <param name="context">The context of the current invocation.</param>
        /// <param name="next">A delegate representing the next step in the execution pipeline (either the next aspect or the target method).</param>
        /// <returns>A <see cref="ValueTask{TResult}"/> representing the result of the execution.</returns>
        ValueTask<TResult> InterceptAsync<TResult>(TAttribute attribute, InvocationContext context, Func<InvocationContext, ValueTask<TResult>> next);
    }
}