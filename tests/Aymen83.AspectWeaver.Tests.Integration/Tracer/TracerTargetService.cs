namespace Aymen83.AspectWeaver.Tests.Integration.Tracer;

public class TracerTargetService(IServiceProvider serviceProvider)
{
    // CRITICAL: Expose IServiceProvider to enable DI integration (Epic 3 requirement, avoids AW001).
    internal IServiceProvider ServiceProvider { get; } = serviceProvider;

    [Tracer]
    public virtual int Calculate(int a, int b)
    {
        return a + b;
    }

    [Tracer]
    public virtual async Task<string> FetchDataAsync(string key)
    {
        await Task.Yield(); // Simulate async work
        if (key == "error")
        {
            throw new InvalidOperationException("Simulated failure");
        }
        return $"Data for {key}";
    }
}