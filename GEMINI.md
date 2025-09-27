# Project: Aymen83.AspectWeaver

## Project Overview

This is a C# project that implements an Aspect-Oriented Programming (AOP) framework for .NET. It leverages C# 12 Source Generators and Interceptors to provide a high-performance, compile-time weaving mechanism for cross-cutting concerns like logging, validation, and resilience. The project is structured into three main parts:

*   **Aymen83.AspectWeaver.Abstractions**: Defines the core interfaces and attributes for creating aspects, such as `AspectAttribute` and `IAspectHandler<T>`.
*   **Aymen83.AspectWeaver.Generator**: The core of the framework, this project contains the C# Source Generator that weaves the aspect logic into the user's code at compile time. It also includes Roslyn Analyzers for providing diagnostics and ensuring correct usage.
*   **Aymen83.AspectWeaver.Extensions**: Provides a set of built-in aspects and extension methods for easy integration with `Microsoft.Extensions.DependencyInjection`. This includes aspects for logging (`[LogExecution]`), parameter validation (`[ValidateParameters]`), and retries (`[Retry]`).

The framework is designed to be clean, modern, and highly performant, with a focus on keeping business logic separate from infrastructural concerns.

## Building and Running

The project is built and tested using the .NET 8 SDK. The following commands can be used from the root of the repository:

*   **Restore Dependencies**:
    ```bash
    dotnet restore
    ```

*   **Build the Solution**:
    ```bash
    dotnet build --configuration Release
    ```

*   **Run Tests**:
    ```bash
    dotnet test --configuration Release
    ```

## Development Conventions

*   **C# 12 and .NET 8**: The project is built using the latest C# and .NET features.
*   **Source Generators**: The core logic is implemented as a C# Source Generator, which requires a good understanding of the Roslyn compiler APIs.
*   **Testing**: The project has a comprehensive test suite, including unit tests for the generator and integration tests for the aspects. The tests are located in the `tests` directory.
*   **Dependency Injection**: The framework is designed to integrate with `Microsoft.Extensions.DependencyInjection`. Aspect handlers can have their own dependencies injected.
*   **Immutability**: The `InvocationContext` and other core data structures are designed to be immutable to ensure thread safety.
