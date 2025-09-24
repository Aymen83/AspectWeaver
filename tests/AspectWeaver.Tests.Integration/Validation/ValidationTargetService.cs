using System;
using AspectWeaver.Extensions.Validation;
using AspectWeaver.Abstractions.Constraints;

namespace AspectWeaver.Tests.Integration.Validation;

public class ValidationTargetService
{
    // Expose IServiceProvider (Epic 3 requirement).
    internal IServiceProvider ServiceProvider { get; }

    public ValidationTargetService(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    // Property used to verify if the method body executed.
    public bool WasExecuted { get; private set; }

    [ValidateParameters]
    public virtual string ProcessData(
        [NotNull] string requiredInput,
        object? optionalInput,
        [NotNull] object requiredObject)
    {
        WasExecuted = true;
        return $"Processed: {requiredInput}";
    }

    // Test case without the [ValidateParameters] aspect.
    public virtual void MethodWithoutValidation([NotNull] string input)
    {
        WasExecuted = true;
        // Constraints should not be enforced if the aspect is missing.
    }
}