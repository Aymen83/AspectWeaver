using Aymen83.AspectWeaver.Extensions.Resilience;

namespace Aymen83.AspectWeaver.Tests.Integration.Resilience;

public class RetryTargetService
{
    // Expose IServiceProvider for the weaver to resolve aspect handlers.
    internal IServiceProvider ServiceProvider { get; } = null!;

    public RetryTargetService(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    /// <summary>
    /// Tracks the number of times the method body has started execution.
    /// </summary>
    public int ExecutionCount { get; private set; }

    /// <summary>
    /// Configurable threshold for when the method should start succeeding.
    /// </summary>
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