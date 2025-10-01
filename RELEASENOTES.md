# 1.0.3

- **Performance:**
    - Implemented a caching mechanism for reflection-based operations in the `ValidateParametersHandler` to reduce runtime overhead.
    - Introduced a caching strategy for `MethodInfo` retrieval in the source generator to optimize the performance of interceptors.
- **Refactoring:**
    - Refactored the `InterceptorEmitter` to encapsulate `MethodInfo` caching logic, leading to cleaner and more maintainable generated code.
    - Improved code comments and documentation across the codebase for better clarity and maintainability.

# 1.0.2

- Initial release of the Aymen83.AspectWeaver framework.
- Includes core abstractions, source generator for aspect weaving, and extensions for dependency injection.
- Built-in aspects for logging, validation, and retries.