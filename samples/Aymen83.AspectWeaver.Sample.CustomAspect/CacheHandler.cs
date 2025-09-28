using Aymen83.AspectWeaver.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using System.Text;

namespace Aymen83.AspectWeaver.Sample.CustomAspect;

public class CacheHandler(IMemoryCache cache) : IAspectHandler<CacheAttribute>
{
    private readonly IMemoryCache _cache = cache;

    public async ValueTask<TResult> InterceptAsync<TResult>(CacheAttribute attribute, InvocationContext context, Func<InvocationContext, ValueTask<TResult>> next)
    {
        // 1. Generate a unique cache key
        var cacheKey = GenerateCacheKey(context);

        // 2. Check the cache
        if (_cache.TryGetValue(cacheKey, out TResult? cachedResult))
        {
            Console.WriteLine($"[Cache Hit] Returning cached result for key: {cacheKey}");
            // We use the null-forgiving operator assuming the cache stores the correct type.
            return cachedResult!;
        }

        // 3. Execute the method if not cached
        Console.WriteLine($"[Cache Miss] Executing method for key: {cacheKey}");
        var result = await next(context);

        // 4. Store the result in the cache
        _cache.Set(cacheKey, result, TimeSpan.FromSeconds(attribute.DurationSeconds));
        Console.WriteLine($"[Cache Set] Stored result for key: {cacheKey}");

        return result;
    }

    private static string GenerateCacheKey(InvocationContext context)
    {
        // Basic key generation: Type.Method|Arg1=Value1|Arg2=Value2...
        var sb = new StringBuilder();
        // Use the generated TargetTypeName (includes global::)
        sb.Append(context.TargetTypeName).Append('.').Append(context.MethodName);

        foreach (var arg in context.Arguments)
        {
            sb.Append('|').Append(arg.Key).Append('=').Append(arg.Value?.ToString() ?? "null");
        }
        return sb.ToString();
    }
}