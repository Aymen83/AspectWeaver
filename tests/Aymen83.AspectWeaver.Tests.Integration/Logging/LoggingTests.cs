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
        var loggerMock = new Mock<ILogger>();
        var loggerFactoryMock = new Mock<ILoggerFactory>();

        // Configure the factory to return the logger mock when the specific target type name is requested.
        loggerFactoryMock.Setup(f => f.CreateLogger("global::" + typeof(LoggingTargetService).FullName!))
                         .Returns(loggerMock.Object);

        // By default, assume all log levels are enabled.
        loggerMock.Setup(l => l.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

        // Register the mocks for retrieval in the constructor.
        services.AddSingleton(loggerMock);
        services.AddSingleton(loggerFactoryMock);

        // Register the ILoggerFactory implementation required by LogExecutionHandler.
        services.AddSingleton<ILoggerFactory>(loggerFactoryMock.Object);

        // Register the target service.
        services.AddTransient<LoggingTargetService>();
    }

    [Fact]
    public void SyncMethod_ShouldLogDetails()
    {
        // Arrange
        var service = GetService<LoggingTargetService>();

        // Act
        var result = service.SyncMethod(42);

        // Assert
        Assert.Equal(84, result);

        // Verify that the logger factory was used to create a logger for the target service.
        _loggerFactoryMock.Verify(f => f.CreateLogger("global::" + typeof(LoggingTargetService).FullName!), Times.Once);

        // Verify that the method execution was logged at the beginning.
        _loggerMock.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Executing method SyncMethod with arguments")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);

        // Verify that the method completion was logged.
        _loggerMock.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Method SyncMethod completed") && v.ToString()!.Contains("with result 84")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task AsyncMethod_Success_ShouldLogDuration()
    {
        // Arrange
        var service = GetService<LoggingTargetService>();

        // Act
        var result = await service.AsyncMethodSuccess("test");

        // Assert
        Assert.Equal("Success: test", result);

        // Verify that the method execution was logged at the beginning.
        _loggerMock.Verify(l => l.Log(
            LogLevel.Debug,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Equals("Executing method AsyncMethodSuccess")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);

        // Verify that the method completion and duration were logged.
        _loggerMock.Verify(l => l.Log(
            LogLevel.Debug,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.StartsWith("Method AsyncMethodSuccess completed in") && v.ToString()!.EndsWith("ms")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task AsyncMethod_Failure_ShouldLogException()
    {
        // Arrange
        var service = GetService<LoggingTargetService>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.AsyncMethodFailure());

        // Verify that the method execution was logged at the beginning.
        _loggerMock.Verify(l => l.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Equals("Executing method AsyncMethodFailure")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);

        // Verify that the exception was logged.
        _loggerMock.Verify(l => l.Log(
            LogLevel.Critical,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.StartsWith("Method AsyncMethodFailure failed after")),
            It.Is<Exception>(e => e == exception), // Ensure the actual exception is passed
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public void DisabledLogLevel_ShouldNotLog()
    {
        // Arrange
        // Override the IsEnabled setup for this specific test case.
        _loggerMock.Setup(l => l.IsEnabled(LogLevel.Trace)).Returns(false);
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