using Xunit;
using Moq;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.DependencyInjection;

namespace AspectWeaver.Tests.Integration.Tracer;

public class TracerTests : IntegrationTestBase
{
    private readonly Mock<ITracerMock> _tracerMock;

    public TracerTests()
    {
        // The constructor runs after IntegrationTestBase constructor, ensuring DI is ready.
        // Retrieve the mock instance from the container.
        _tracerMock = GetService<Mock<ITracerMock>>();
    }

    // Register mocks and services required for the tests.
    protected override void ConfigureServices(IServiceCollection services)
    {
        // Register the mock itself so it can be retrieved in the test constructor.
        var mock = new Mock<ITracerMock>();
        services.AddSingleton(mock);
        // Register the interface implementation so it can be injected into the TracerHandler.
        services.AddSingleton(mock.Object);

        // Register the target service.
        services.AddTransient<TracerTargetService>();
    }

    [Fact]
    public void PBI4_1_Test_SynchronousMethod_ShouldBeTraced()
    {
        // Arrange
        var service = GetService<TracerTargetService>();

        // Act
        // This call is intercepted by the woven code.
        var result = service.Calculate(10, 5);

        // Assert
        Assert.Equal(15, result);

        // Verify that the mock was called correctly by the aspect handler.
        _tracerMock.Verify(m => m.Trace("Before Calculate"), Times.Once);
        _tracerMock.Verify(m => m.Trace("After Calculate"), Times.Once);
        _tracerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PBI4_1_Test_AsynchronousMethod_Success_ShouldBeTraced()
    {
        // Arrange
        var service = GetService<TracerTargetService>();

        // Act
        var result = await service.FetchDataAsync("test_key");

        // Assert
        Assert.Equal("Data for test_key", result);

        // Verify
        _tracerMock.Verify(m => m.Trace("Before FetchDataAsync"), Times.Once);
        _tracerMock.Verify(m => m.Trace("After FetchDataAsync"), Times.Once);
        _tracerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PBI4_1_Test_AsynchronousMethod_Failure_ShouldBeTraced()
    {
        // Arrange
        var service = GetService<TracerTargetService>();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.FetchDataAsync("error"));
        Assert.Equal("Simulated failure", exception.Message);

        // Verify
        _tracerMock.Verify(m => m.Trace("Before FetchDataAsync"), Times.Once);
        // Ensure the exception path was traced.
        _tracerMock.Verify(m => m.Trace("Exception in FetchDataAsync: Simulated failure"), Times.Once);
        // Ensure the success path was NOT traced.
        _tracerMock.Verify(m => m.Trace("After FetchDataAsync"), Times.Never);
        _tracerMock.VerifyNoOtherCalls();
    }
}