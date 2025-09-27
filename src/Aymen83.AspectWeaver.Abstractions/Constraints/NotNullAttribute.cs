using System;

namespace Aymen83.AspectWeaver.Abstractions.Constraints
{
    /// <summary>
    /// Specifies that the annotated parameter must not be null.
    /// This constraint is enforced when the containing method is annotated with an aspect that performs validation,
    /// such as [ValidateParameters].
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
    public sealed class NotNullAttribute : Attribute
    {
    }
}