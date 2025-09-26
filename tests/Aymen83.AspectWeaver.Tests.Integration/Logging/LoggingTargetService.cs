using Aymen83.AspectWeaver.Extensions.Logging;
using Microsoft.Extensions.Logging;

namespace Aymen83.AspectWeaver.Tests.Integration.Logging;

public class LoggingTargetService(IServiceProvider serviceProvider)
{
    // CRITICAL: Expose IServiceProvider (Epic 3 requirement).
    internal IServiceProvider ServiceProvider { get; } = serviceProvider;

    [LogExecution(Level = LogLevel.Information, LogArguments = true, LogReturnValue = true)]
    public virtual int SyncMethod(int input)
    {
        return input * 2;
    }

    [LogExecution(Level = LogLevel.Debug)]
    public virtual async Task<string> AsyncMethodSuccess(string key)
    {
        await Task.Delay(10); // Simulate work
        return $"Success: {key}";
    }

    [LogExecution(Level = LogLevel.Warning, ExceptionLevel = LogLevel.Critical)]
    public virtual async Task AsyncMethodFailure()
    {
        await Task.Delay(5);
        throw new InvalidOperationException("Operation failed");
    }

    // Test case for disabled logging level.
    [LogExecution(Level = LogLevel.Trace)]
    public virtual void DisabledLogLevelMethod()
    {
        // Should not log if the configured level is higher than Trace.
    }
}