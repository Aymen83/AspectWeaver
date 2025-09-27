using Microsoft.Extensions.DependencyInjection;

namespace Aymen83.AspectWeaver.Tests.Integration.Validation;

public class ValidationTests : IntegrationTestBase
{
    protected override void ConfigureServices(IServiceCollection services)
    {
        // The ValidateParametersHandler is automatically registered by IntegrationTestBase's assembly scanning.
        services.AddTransient<ValidationTargetService>();
    }

    [Fact]
    public void ValidInput_ShouldExecuteNormally()
    {
        // Arrange
        var service = GetService<ValidationTargetService>();

        // Act
        var result = service.ProcessData("Test", null, new object());

        // Assert
        Assert.Equal("Processed: Test", result);
        Assert.True(service.WasExecuted);
    }

    [Fact]
    public void NullRequiredInput_ShouldThrowAndShortCircuit()
    {
        // Arrange
        var service = GetService<ValidationTargetService>();

        // Act & Assert
        // Verify that the correct exception is thrown for the first violating parameter.
        var exception = Assert.Throws<ArgumentNullException>(() => service.ProcessData(null!, null, new object()));

        Assert.Equal("requiredInput", exception.ParamName);
        Assert.Contains("Parameter 'requiredInput' cannot be null", exception.Message);

        // Verify short-circuiting: The method body must not have executed.
        Assert.False(service.WasExecuted);
    }

    [Fact]
    public void NullRequiredObject_ShouldThrow()
    {
        // Arrange
        var service = GetService<ValidationTargetService>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => service.ProcessData("Test", null, null!));

        Assert.Equal("requiredObject", exception.ParamName);
        Assert.False(service.WasExecuted);
    }

    [Fact]
    public void WithoutAspect_ShouldNotValidate()
    {
        // Arrange
        var service = GetService<ValidationTargetService>();

        // Act
        // This should not throw even if the input is null, because [ValidateParameters] is missing.
        service.MethodWithoutValidation(null!);

        // Assert
        Assert.True(service.WasExecuted);
    }
}