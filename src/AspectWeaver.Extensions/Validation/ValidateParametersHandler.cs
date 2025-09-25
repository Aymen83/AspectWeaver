// src/AspectWeaver.Extensions/Validation/ValidateParametersHandler.cs
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using AspectWeaver.Abstractions;
using AspectWeaver.Abstractions.Constraints;

namespace AspectWeaver.Extensions.Validation
{
    /// <summary>
    /// Handler for <see cref="ValidateParametersAttribute"/>.
    /// Uses reflection to enforce constraints defined on parameters.
    /// </summary>
    public sealed class ValidateParametersHandler : IAspectHandler<ValidateParametersAttribute>
    {
        // This handler does not require DI.

        /// <inheritdoc />
        public async ValueTask<TResult> InterceptAsync<TResult>(ValidateParametersAttribute attribute, InvocationContext context, Func<InvocationContext, ValueTask<TResult>> next)
        {
            // 1. Retrieve parameter metadata using reflection (PBI 4.2).
            var parameters = context.MethodInfo.GetParameters();

            // 2. Iterate through the parameters defined on the method.
            foreach (var parameterInfo in parameters)
            {
                // 3. Correlate metadata with the runtime argument value.
                if (context.Arguments.TryGetValue(parameterInfo.Name, out var argumentValue))
                {
                    // 4. Check for [NotNull] constraint violation.
                    if (argumentValue == null && HasNotNullConstraint(parameterInfo))
                    {
                        // 5. Short-circuit execution and throw exception.
                        throw new ArgumentNullException(parameterInfo.Name, $"Parameter '{parameterInfo.Name}' cannot be null in method '{context.MethodName}'.");
                    }
                }
            }

            // 6. If validation passes, proceed with the execution chain.
            return await next(context).ConfigureAwait(false);
        }

        private static bool HasNotNullConstraint(ParameterInfo parameterInfo)
        {
            // Check if the [NotNullAttribute] is applied to the parameter.
            // We use GetCustomAttributes for compatibility with .NET Standard 2.0.
            return parameterInfo.GetCustomAttributes(typeof(NotNullAttribute), inherit: false).Any();
        }
    }
}