### New Rules

Rule ID | Category          | Severity | Notes
--------|-------------------|----------|-------
AW001   | AspectWeaver.DI   | Error    | Aspect handlers require resolution via IServiceProvider. The generator must be able to access the provider from the intercepted instance.
AW002   | AspectWeaver.DI   | Error    | Aspect weaving with dependency injection relies on accessing IServiceProvider from the target instance, which is unavailable for static methods.