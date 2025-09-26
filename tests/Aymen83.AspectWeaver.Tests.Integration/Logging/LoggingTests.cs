using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Aymen83.AspectWeaver.Tests.Integration.Logging;

public class LoggingTests : IntegrationTestBase
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;

    public LoggingTests()
    {
        // Retrieve the mocks registered in ConfigureServices.
        _loggerMock = GetService<Mock<ILogger>>();
        _loggerFactoryMock = GetService<Mock<ILoggerFactory>>();
    }

    // Register mocks and services required for the tests.
    protected override void ConfigureServices(IServiceCollection services)
    {
        // Setup ILogger and ILoggerFactory mocks.
        var loggerMock = new Mock<ILogger>();
        var loggerFactoryMock = new Mock<ILoggerFactory>();

        // Configure the factory to return the logger mock when the specific target type name is requested.
        // Use the non-nullable assertion (!) for FullName as we know the type is defined.
        loggerFactoryMock.Setup(f => f.CreateLogger("global::" + typeof(LoggingTargetService).FullName!))
                         .Returns(loggerMock.Object);

        // Default setup for IsEnabled: Assume all levels are enabled unless explicitly overridden in a test.
        loggerMock.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

        // Register the mocks themselves (for retrieval in constructor).
        services.AddSingleton(loggerMock);
        services.AddSingleton(loggerFactoryMock);

        // Register the ILoggerFactory implementation (required by LogExecutionHandler).
        services.AddSingleton<ILoggerFactory>(loggerFactoryMock.Object);

        // Register the target service.
        services.AddTransient<LoggingTargetService>();
    }

    [Fact]
    public void PBI4_3_SyncMethod_ShouldLogDetails()
    {
        // Arrange
        var service = GetService<LoggingTargetService>();

        // Act
        var result = service.SyncMethod(42);

        // Assert
        Assert.Equal(84, result);

        // Verify LoggerFactory interaction
        _loggerFactoryMock.Verify(f => f.CreateLogger("global::" + typeof(LoggingTargetService).FullName!), Times.Once);

        // Verify Logger interactions (Entry)
        // We use Moq's flexible matching for structured logging verification.
        _loggerMock.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Executing method SyncMethod with arguments")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);

        // Verify Logger interactions (Exit)
        _loggerMock.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Method SyncMethod completed") && v.ToString()!.Contains("with result 84")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task PBI4_3_AsyncMethod_Success_ShouldLogDuration()
    {
        // Arrange
        var service = GetService<LoggingTargetService>();

        // Act
        var result = await service.AsyncMethodSuccess("test");

        // Assert
        Assert.Equal("Success: test", result);

        // Verify Logger interactions (Entry - Debug Level, No Arguments)
        _loggerMock.Verify(l => l.Log(
            LogLevel.Debug,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Equals("Executing method AsyncMethodSuccess")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);

        // Verify Logger interactions (Exit - Debug Level, Duration, No Result)
        _loggerMock.Verify(l => l.Log(
            LogLevel.Debug,
            It.IsAny<EventId>(),
            // Check that the duration is logged (ends with ms).
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.StartsWith("Method AsyncMethodSuccess completed in") && v.ToString()!.EndsWith("ms")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task PBI4_3_AsyncMethod_Failure_ShouldLogException()
    {
        // Arrange
        var service = GetService<LoggingTargetService>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.AsyncMethodFailure());

        // Verify Logger interactions (Entry - Warning Level)
        _loggerMock.Verify(l => l.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Equals("Executing method AsyncMethodFailure")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);

        // Verify Logger interactions (Exception - Critical Level)
        _loggerMock.Verify(l => l.Log(
            LogLevel.Critical,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.StartsWith("Method AsyncMethodFailure failed after")),
            It.Is<Exception>(e => e == exception), // Ensure the actual exception is passed
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public void PBI4_3_DisabledLogLevel_ShouldNotLog()
    {
        // Arrange
        // Override the IsEnabled setup for this specific test case.
        _loggerMock.Setup(l => l.IsEnabled(LogLevel.Trace)).Returns(false);
        // Ensure the exception level is also disabled to validate the optimization check.
        _loggerMock.Setup(l => l.IsEnabled(LogLevel.Error)).Returns(false);

        var service = GetService<LoggingTargetService>();

        // Act
        service.DisabledLogLevelMethod();

        // Assert
        // Verify that IsEnabled was checked for both standard level and exception level.
        _loggerMock.Verify(l => l.IsEnabled(LogLevel.Trace), Times.AtLeastOnce);
        _loggerMock.Verify(l => l.IsEnabled(LogLevel.Error), Times.AtLeastOnce);

        // Verify that the logger methods were NOT called (optimization check).
        _loggerMock.Verify(l => l.Log(
            It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Never);
    }
}