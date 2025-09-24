using Xunit;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace AspectWeaver.Tests.Integration.Resilience;

public class RetryTests : IntegrationTestBase
{
    // Register the target service.
    protected override void ConfigureServices(IServiceCollection services)
    {
        // The RetryHandler is automatically registered by IntegrationTestBase assembly scanning.
        services.AddTransient<RetryTargetService>();
    }

    [Fact]
    public async Task PBI4_5_Async_SuccessOnFirstTry_ShouldNotRetry()
    {
        // Arrange
        var service = GetService<RetryTargetService>();
        service.SucceedOnAttempt = 1;

        // Act
        var result = await service.PerformOperationAsync();

        // Assert
        Assert.Equal("Success on attempt 1.", result);
        Assert.Equal(1, service.ExecutionCount);
    }

    [Fact]
    public async Task PBI4_5_Async_SuccessOnRetry_ShouldExecuteMultipleTimesAndRespectDelay()
    {
        // Arrange
        var service = GetService<RetryTargetService>();
        service.SucceedOnAttempt = 3; // Fails 2 times, succeeds on the 3rd.
        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await service.PerformOperationAsync();
        stopwatch.Stop();

        // Assert
        Assert.Equal("Success on attempt 3.", result);
        Assert.Equal(3, service.ExecutionCount);

        // Verify duration: 2 failures * 50ms delay = at least 100ms total time.
        Assert.True(stopwatch.ElapsedMilliseconds >= 100, $"Expected duration >= 100ms, actual: {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task PBI4_5_Async_FailureAfterExhaustion_ShouldThrowLastException()
    {
        // Arrange
        var service = GetService<RetryTargetService>();
        // Configure to fail beyond the MaxAttempts (which is 5).
        service.SucceedOnAttempt = 6;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<TimeoutException>(() => service.PerformOperationAsync());

        // Verify the final exception message corresponds to the last attempt.
        Assert.Equal("Transient failure on attempt 5.", exception.Message);
        // Verify the execution happened exactly 5 times.
        Assert.Equal(5, service.ExecutionCount);
    }

    [Fact]
    public void PBI4_5_Sync_SuccessOnRetry_ShouldRetry()
    {
        // Arrange
        var service = GetService<RetryTargetService>();
        service.SucceedOnAttempt = 2; // Default MaxAttempts is 3.

        // Act
        var result = service.PerformSyncOperation();

        // Assert
        Assert.Equal("Sync Success on attempt 2.", result);
        Assert.Equal(2, service.ExecutionCount);
    }

    [Fact]
    public void PBI4_5_Sync_FailureAfterExhaustion_ShouldThrow()
    {
        // Arrange
        var service = GetService<RetryTargetService>();
        service.SucceedOnAttempt = 4; // Default MaxAttempts is 3.

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => service.PerformSyncOperation());

        Assert.Equal("Sync failure on attempt 3.", exception.Message);
        Assert.Equal(3, service.ExecutionCount);
    }
}