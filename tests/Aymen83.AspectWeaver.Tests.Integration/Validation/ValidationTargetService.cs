using Aymen83.AspectWeaver.Abstractions.Constraints;
using Aymen83.AspectWeaver.Extensions.Validation;

namespace Aymen83.AspectWeaver.Tests.Integration.Validation;

public class ValidationTargetService(IServiceProvider serviceProvider)
{
    // Expose IServiceProvider for the weaver to resolve aspect handlers.
    internal IServiceProvider ServiceProvider { get; } = serviceProvider;

    // This property is used by tests to verify if the method body was executed.
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

    // This method is used to test that constraints are not enforced if the [ValidateParameters] aspect is missing.
    public virtual void MethodWithoutValidation([NotNull] string input)
    {
        WasExecuted = true;
    }
}