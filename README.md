# Aymen83.AspectWeaver

[![Build and Test](https://github.com/Aymen83/AspectWeaver/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/Aymen83/AspectWeaver/actions/workflows/build-and-test.yml)
[![NuGet Version (Aymen83.AspectWeaver)](https://img.shields.io/nuget/v/AspectWeaver.svg?label=AspectWeaver)](https://www.nuget.org/packages/AspectWeaver/)
[![NuGet Version (Aymen83.AspectWeaver.Extensions)](https://img.shields.io/nuget/v/AspectWeaver.Extensions.svg?label=AspectWeaver.Extensions)](https://www.nuget.org/packages/AspectWeaver.Extensions/)

Aymen83.AspectWeaver is a modern, high-performance Aspect-Oriented Programming (AOP) framework for .NET, leveraging the power of C# 12 Source Generators and Interceptors. It enables developers to implement cross-cutting concerns (like logging, validation, and resilience) cleanly without polluting the business logic.

## Key Features

- **High Performance:** Utilizes C# 12 Interceptors for near-native performance during method calls.
- **Compile-Time Weaving:** Aspects are woven during the build process, ensuring optimal runtime performance.
- **Integrated Diagnostics:** Includes Roslyn Analyzers to provide immediate feedback and validation of aspect usage.
- **Full Async Support:** Seamlessly handles `async/await`, `Task`, and `ValueTask` methods.
- **DI Integration:** Integrates fully with `Microsoft.Extensions.DependencyInjection`, allowing aspect handlers to have their own dependencies.
- **Generic Support:** Robustly handles complex generic methods and constraints.
- **Clean Architecture:** Keeps business logic decoupled from infrastructure concerns.

## Prerequisites

- **.NET 8.0 SDK** or newer.
- **C# 12** (Enabled by default in .NET 8+).

## Installation

Install the core Aymen83.AspectWeaver package (which includes the generator and abstractions) and the Extensions package (for built-in aspects and DI helpers).

```bash
dotnet add package Aymen83.AspectWeaver
dotnet add package Aymen83.AspectWeaver.Extensions
```

### Important Configuration Note (C# 12 Interceptors)

Aymen83.AspectWeaver relies on C# 12 Interceptors. The configuration required depends on the SDK you are using to compile the project:

#### Compiling with .NET 8 SDK (e.g., Visual Studio 2022)

If you are compiling using the .NET 8 SDK, Interceptors are considered a **preview feature** by that SDK version and must be explicitly enabled. Add the following to the `.csproj` file of any project where you intend to use aspects:

```xml
<PropertyGroup>
  <Features>InterceptorsPreview</Features>
</PropertyGroup>
```

#### Compiling with .NET 9 SDK (or newer)

If you are compiling using the .NET 9 SDK or later, the `<Features>InterceptorsPreview</Features>` flag should **not** be required, as the feature is expected to be stable.

## Usage Guide

### 1. Define an Aspect

Create an attribute deriving from `AspectAttribute` and a corresponding handler implementing `IAspectHandler<T>`.

```csharp
using Aymen83.AspectWeaver.Abstractions;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

// 1. The Attribute
public class SimpleLogAttribute : AspectAttribute { }

// 2. The Handler
public class SimpleLogHandler : IAspectHandler<SimpleLogAttribute>
{
    private readonly ILogger<SimpleLogHandler> _logger;

    // Inject dependencies via DI
    public SimpleLogHandler(ILogger<SimpleLogHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<TResult> InterceptAsync<TResult>(
        SimpleLogAttribute attribute,
        InvocationContext context,
        Func<InvocationContext, ValueTask<TResult>> next)
    {
        _logger.LogInformation("Before executing {Method}", context.MethodName);
        var result = await next(context);
        _logger.LogInformation("After executing {Method}", context.MethodName);
        return result;
    }
}
```

### 2. Register Handlers (DI)

Use the extension methods provided by `Aymen83.AspectWeaver.Extensions` to register your handlers in the DI container.

```csharp
using Aymen83.AspectWeaver.Extensions;

// In your DI configuration (e.g., Program.cs)
builder.Services.AddLogging();

// Scan the assembly containing 'Program' for IAspectHandler implementations
builder.Services.AddAspectWeaverHandlers<Program>();
// Or scan a specific assembly:
// builder.Services.AddAspectWeaverHandlers(typeof(SimpleLogHandler).Assembly);
```

### 3. Configure Target Services (Crucial Step)

For Aymen83.AspectWeaver to resolve handlers via DI, the class containing the intercepted methods **must** expose an accessible `IServiceProvider`. This is a requirement of the framework's architecture.

```csharp
public class MyService
{
    // Expose the IServiceProvider (internal or public accessibility is required)
    internal IServiceProvider ServiceProvider { get; }

    // The DI container automatically injects the provider during construction.
    public MyService(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    // ... methods ...
}
```

If the `IServiceProvider` is missing or inaccessible (e.g., `private`), the generator will emit a compile-time error (**AW001**).

**Interfaces:** If aspects are applied on an interface method, the interface must also expose the `IServiceProvider` contract:

```csharp
public interface IMyService
{
    IServiceProvider ServiceProvider { get; }
    // ... methods ...
}
```

### 4. Apply Aspects

Apply the attributes to your methods. The generator will automatically weave the interception logic at compile time.

```csharp
public class MyService
{
    // ... (IServiceProvider setup) ...

    // Apply the aspect
    [SimpleLog]
    public virtual async Task ProcessDataAsync(string data)
    {
        // Business logic...
    }
}
```

When `ProcessDataAsync` is called anywhere in your application, the `SimpleLogHandler` will automatically execute around it.

## Built-in Aspects (Aymen83.AspectWeaver.Extensions)

The `Aymen83.AspectWeaver.Extensions` package includes several ready-to-use aspects:

### `[LogExecution]`

Logs the start, end, duration, and exceptions of a method execution using `ILogger`.

```csharp
using Aymen83.AspectWeaver.Extensions.Logging;
using Microsoft.Extensions.Logging;

[LogExecution(Level = LogLevel.Debug, LogArguments = true, LogReturnValue = false)]
public virtual int Calculate(int a, int b) { ... }
```

### `[ValidateParameters]` and `[NotNull]`

Provides automatic null checking for parameters marked with `[NotNull]`. Throws `ArgumentNullException` and short-circuits execution if a violation occurs.

```csharp
using Aymen83.AspectWeaver.Abstractions.Constraints;
using Aymen83.AspectWeaver.Extensions.Validation;

[ValidateParameters] // Enables validation for this method
public virtual string FormatName(
    [NotNull] string firstName,
    string? middleName,
    [NotNull] string lastName)
{
    // ...
}
```

### `[Retry]`

Implements a simple fixed-delay retry policy for handling transient failures.

```csharp
using Aymen83.AspectWeaver.Extensions.Resilience;

[Retry(MaxAttempts = 5, DelayMilliseconds = 200)]
public virtual async Task<string> FetchFromExternalApiAsync() { ... }
```

## Diagnostics Reference

Aymen83.AspectWeaver includes dedicated Roslyn Analyzers and diagnostics within the Source Generator to provide immediate feedback:

| ID | Severity | Description |
| :--- | :--- | :--- |
| **AW001** | Error | `IServiceProvider` not found or inaccessible on the target instance/interface. |
| **AW002** | Error | Aspects applied to static methods (not supported due to DI requirement). |
| **AW003** | Error | Aspect applied to an invalid target (e.g., Property, Field, Constructor). |
| **AW004** | Warning | Uninterceptable call pattern (e.g., `base.` calls). The aspect will not run for this specific call site due to C# limitations. |
| **AW005** | Error | Invalid attribute configuration (e.g., `[Retry(MaxAttempts = 0)]`). |
| **AW006** | Error | Methods using `ref struct` parameters (e.g., `Span<T>`) are not supported as they cannot be safely captured. |

## License

This project is licensed under the MIT License.