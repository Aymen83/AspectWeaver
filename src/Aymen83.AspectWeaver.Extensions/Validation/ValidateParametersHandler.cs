using Aymen83.AspectWeaver.Abstractions;
using Aymen83.AspectWeaver.Abstractions.Constraints;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Aymen83.AspectWeaver.Extensions.Validation
{
    /// <summary>
    /// Handler for <see cref="ValidateParametersAttribute"/>.
    /// Uses reflection to enforce constraints defined on parameters.
    /// </summary>
    public sealed class ValidateParametersHandler : IAspectHandler<ValidateParametersAttribute>
    {
        // Optimization: Cache metadata analysis results.
        // Key: MethodInfo. Value: ImmutableArray of ParameterMetadata.
        // ConcurrentDictionary ensures thread safety and high performance.
        private static readonly ConcurrentDictionary<MethodInfo, ImmutableArray<ParameterMetadata>> ValidationCache = new();

        /// <inheritdoc />
        public async ValueTask<TResult> InterceptAsync<TResult>(ValidateParametersAttribute attribute, InvocationContext context, Func<InvocationContext, ValueTask<TResult>> next)
        {
            // 1. Retrieve or analyze metadata (Optimized Caching).
            // GetOrAdd ensures that AnalyzeMethodMetadata runs only once per MethodInfo atomically.
            var metadataList = ValidationCache.GetOrAdd(context.MethodInfo, AnalyzeMethodMetadata);

            // 2. Execute validation based on the cached metadata (Fast path).
            foreach (var metadata in metadataList)
            {
                            // We use the indexer [] for performance, relying on the generator to ensure the argument exists.
                            var argumentValue = context.Arguments[metadata.Name];
                // Check for [NotNull] constraint violation.
                if (argumentValue == null && metadata.IsNotNull)
                {
                    // Short-circuit execution and throw exception.
                    throw new ArgumentNullException(metadata.Name, $"Parameter '{metadata.Name}' cannot be null in method '{context.MethodName}'.");
                }
            }

            // 3. If validation passes, proceed with the execution chain.
            return await next(context).ConfigureAwait(false);
        }

        // Analyzes the method metadata once. This is where the reflection cost occurs.
        private static ImmutableArray<ParameterMetadata> AnalyzeMethodMetadata(MethodInfo methodInfo)
        {
            var parameters = methodInfo.GetParameters();
            var metadataList = new List<ParameterMetadata>(parameters.Length);

            foreach (var parameterInfo in parameters)
            {
                // Optimization: We only analyze parameters that can be null (reference types or Nullable<T>).
                // This check is compatible with .NET Standard 2.0.
                // The null check on parameterInfo.Name is for safety, as it should always be present.
                if (parameterInfo.Name != null && (parameterInfo.ParameterType.IsClass || Nullable.GetUnderlyingType(parameterInfo.ParameterType) != null))
                {
                    // Check for [NotNullAttribute] using reflection.
                    var isNotNull = parameterInfo.GetCustomAttributes(typeof(NotNullAttribute), inherit: false).Any();

                    // Store the analyzed result.
                    metadataList.Add(new ParameterMetadata(parameterInfo.Name, isNotNull));
                }
            }

            return metadataList.ToImmutableArray();
        }

        // Helper struct to store the analyzed metadata efficiently.
        private readonly struct ParameterMetadata(string name, bool isNotNull)
        {
            public readonly string Name = name;
            public readonly bool IsNotNull = isNotNull;
        }
    }
}