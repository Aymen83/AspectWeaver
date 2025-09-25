### New Rules

Rule ID | Category                   | Severity | Notes
--------|----------------------------|----------|-------
AW001   | AspectWeaver.DI            | Error    | Aspect handlers require resolution via IServiceProvider. The generator must be able to access the provider from the intercepted instance.
AW002   | AspectWeaver.DI            | Error    | Aspect weaving with dependency injection relies on accessing IServiceProvider from the target instance, which is unavailable for static methods.
AW003   | AspectWeaver.Usage         | Error    | AspectAttributes derived from AspectWeaver.Abstractions.AspectAttribute can only be applied to methods.
AW004   | AspectWeaver.Limitations   | Warning  | C# 12 Interceptors have limitations on which call patterns can be redirected. Calls using 'base.' access are executed directly.
AW005   | AspectWeaver.Configuration | Error    | The configuration values provided for the attribute are outside the allowed range or invalid.
AW006   | AspectWeaver.Limitations   | Error    | The AspectWeaver pipeline requires capturing arguments, which is not safely possible with 'ref struct' types.