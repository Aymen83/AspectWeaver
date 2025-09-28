using Aymen83.AspectWeaver.Abstractions.Constraints;
using Aymen83.AspectWeaver.Extensions.Logging;
using Aymen83.AspectWeaver.Extensions.Resilience;
using Aymen83.AspectWeaver.Extensions.Validation;

namespace Aymen83.AspectWeaver.Sample.MinimalApi;

public interface IWeatherService
{
    // Required by AspectWeaver for DI (AW001) when calling via interface
    IServiceProvider ServiceProvider { get; }

    // Apply aspects: Execution Order (Validation -> Logging -> Retry -> Method)
    [ValidateParameters] // Order -1000
    // LogExecution has Order 100
    [LogExecution(Level = LogLevel.Information, LogArguments = true)]
    // Retry has Order 1000 (wraps Logging)
    [Retry(MaxAttempts = 3, DelayMilliseconds = 100)]
    Task<string> GetWeatherAsync([NotNull] string city);
}