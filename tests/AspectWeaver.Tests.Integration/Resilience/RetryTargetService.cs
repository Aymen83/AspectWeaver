using AspectWeaver.Extensions.Resilience;

namespace AspectWeaver.Tests.Integration.Resilience;

public class RetryTargetService(IServiceProvider serviceProvider)
{
    // Expose IServiceProvider (Epic 3 requirement).
    internal IServiceProvider ServiceProvider { get; } = serviceProvider;

    // Tracks the number of times the method body has started execution.
    public int ExecutionCount { get; private set; }

    // Configurable threshold for when the method should start succeeding.
    public int SucceedOnAttempt { get; set; } = 1;

    [Retry(MaxAttempts = 5, DelayMilliseconds = 50)]
    public virtual async Task<string> PerformOperationAsync()
    {
        ExecutionCount++;

        // Simulate asynchronous work.
        await Task.Yield();

        if (ExecutionCount < SucceedOnAttempt)
        {
            throw new TimeoutException($"Transient failure on attempt {ExecutionCount}.");
        }

        return $"Success on attempt {ExecutionCount}.";
    }

    // Test case for synchronous methods with default configuration (3 attempts, 100ms delay).
    [Retry]
    public virtual string PerformSyncOperation()
    {
        ExecutionCount++;

        if (ExecutionCount < SucceedOnAttempt)
        {
            throw new InvalidOperationException($"Sync failure on attempt {ExecutionCount}.");
        }

        return $"Sync Success on attempt {ExecutionCount}.";
    }
}