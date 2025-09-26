using Aymen83.AspectWeaver.Abstractions;

namespace Aymen83.AspectWeaver.Tests.Integration.Tracer;

// 1. Define the Attribute
[AttributeUsage(AttributeTargets.Method)]
public class TracerAttribute : AspectAttribute { }

// 2. Define the Handler (This will be registered by IntegrationTestBase via assembly scanning)
public class TracerHandler(ITracerMock mock) : IAspectHandler<TracerAttribute>
{
    private readonly ITracerMock _mock = mock;

    public async ValueTask<TResult> InterceptAsync<TResult>(TracerAttribute attribute, InvocationContext context, Func<InvocationContext, ValueTask<TResult>> next)
    {
        _mock.Trace($"Before {context.MethodName}");
        try
        {
            // Proceed with execution
            var result = await next(context).ConfigureAwait(false);
            _mock.Trace($"After {context.MethodName}");
            return result;
        }
        catch (Exception ex)
        {
            // Trace exceptions
            _mock.Trace($"Exception in {context.MethodName}: {ex.Message}");
            throw;
        }
    }
}