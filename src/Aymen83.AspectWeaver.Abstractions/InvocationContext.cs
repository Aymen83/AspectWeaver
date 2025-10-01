using System;
using System.Reflection;

namespace Aymen83.AspectWeaver.Abstractions
{
    /// <summary>
    /// Provides context information about the intercepted method invocation.
    /// This context is passed through the chain of aspect handlers.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="InvocationContext"/> class.
    /// </remarks>
    /// <param name="targetInstance">The instance of the object on which the method is being invoked, or null if static.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> associated with the current scope.</param>
    /// <param name="methodInfo">The <see cref="System.Reflection.MethodInfo"/> representing the intercepted method.</param>
    /// <param name="methodName">The name of the intercepted method.</param>
    /// <param name="targetTypeName">The full name of the type containing the intercepted method.</param>
    /// <param name="arguments">A dictionary containing the arguments passed to the method.</param>
    public sealed class InvocationContext(
        object? targetInstance,
        IServiceProvider serviceProvider,
        MethodInfo methodInfo,
        string methodName,
        string targetTypeName,
        IArgumentsContainer arguments)
    {
        /// <summary>
        /// Gets the instance of the object on which the method is being invoked.
        /// Returns null if the intercepted method is static.
        /// </summary>
        public object? TargetInstance { get; } = targetInstance;

        /// <summary>
        /// Gets the <see cref="IServiceProvider"/> associated with the current execution scope.
        /// Used by handlers to resolve dependencies.
        /// </summary>
        public IServiceProvider ServiceProvider { get; } = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        /// <summary>
        /// Gets the name of the intercepted method.
        /// </summary>
        public string MethodName { get; } = methodName ?? throw new ArgumentNullException(nameof(methodName));

        /// <summary>
        /// Gets the full name of the type containing the intercepted method.
        /// Useful for creating specific loggers.
        /// </summary>
        public string TargetTypeName { get; } = targetTypeName ?? throw new ArgumentNullException(nameof(targetTypeName));

        /// <summary>
        /// Gets a read-only dictionary containing the arguments passed to the intercepted method.
        /// </summary>
        public IArgumentsContainer Arguments { get; } = arguments ?? throw new ArgumentNullException(nameof(arguments));

        /// <summary>
        /// Gets the <see cref="System.Reflection.MethodInfo"/> representing the intercepted method.
        /// This allows access to method attributes, parameters metadata, and generic type arguments.
        /// </summary>
        public MethodInfo MethodInfo { get; } = methodInfo ?? throw new ArgumentNullException(nameof(methodInfo));
    }
}