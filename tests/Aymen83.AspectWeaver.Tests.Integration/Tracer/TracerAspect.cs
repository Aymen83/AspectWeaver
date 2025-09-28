using Aymen83.AspectWeaver.Abstractions;

namespace Aymen83.AspectWeaver.Tests.Integration.Tracer;

/// <summary>
/// Defines the attribute used to mark methods for tracing.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class TracerAttribute : AspectAttribute { }

/// <summary>
/// Handles the logic for the <see cref="TracerAttribute"/>.
/// This will be registered by IntegrationTestBase via assembly scanning.
/// </summary>
public class TracerHandler(ITracerMock mock) : IAspectHandler<TracerAttribute>
{
    private readonly ITracerMock _mock = mock;

    public async ValueTask<TResult> InterceptAsync<TResult>(TracerAttribute attribute, InvocationContext context, Func<InvocationContext, ValueTask<TResult>> next)
    {
        _mock.Trace($"Before {context.MethodName}");
        try
        {
            var result = await next(context).ConfigureAwait(false);
            _mock.Trace($"After {context.MethodName}");
            return result;
        }
        catch (Exception ex)
        {
            _mock.Trace($"Exception in {context.MethodName}: {ex.Message}");
            throw;
        }
    }
}
