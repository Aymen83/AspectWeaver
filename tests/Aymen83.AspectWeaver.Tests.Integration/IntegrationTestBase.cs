using Aymen83.AspectWeaver.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Aymen83.AspectWeaver.Tests.Integration;

/// <summary>
/// Base class for integration tests, providing DI container setup and isolation.
/// </summary>
public abstract class IntegrationTestBase : IDisposable
{
    private readonly ServiceProvider _serviceProvider;

    /// <summary>
    /// Provides access to the configured IServiceCollection before the container is built.
    /// </summary>
    protected IServiceCollection Services { get; }

    /// <summary>
    /// Provides access to the built IServiceProvider.
    /// </summary>
    protected IServiceProvider ServiceProvider => _serviceProvider;

    protected IntegrationTestBase()
    {
        Services = new ServiceCollection();

        // 1. Register Handlers
        Services.AddAspectWeaverHandlers(typeof(AspectWeaverServiceCollectionExtensions).Assembly);
        Services.AddAspectWeaverHandlers(typeof(IntegrationTestBase).Assembly);

        // 2. Allow derived tests to register mocks and services.
        ConfigureServices(Services);

        // 3. Build the container.
        _serviceProvider = Services.BuildServiceProvider();
    }

    /// <summary>
    /// Override this method in derived tests to register specific services and mocks.
    /// </summary>
    protected virtual void ConfigureServices(IServiceCollection services)
    {
    }

    /// <summary>
    /// Resolves a service from the DI container.
    /// </summary>
    protected T GetService<T>() where T : notnull
    {
        return _serviceProvider.GetRequiredService<T>();
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
        GC.SuppressFinalize(this);
    }
}