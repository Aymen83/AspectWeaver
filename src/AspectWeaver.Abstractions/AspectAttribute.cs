// Necessary because ImplicitUsings is disabled for .NET Standard 2.0 (see Directory.Build.props)
using System;

namespace AspectWeaver.Abstractions
{
    /// <summary>
    /// Base class for all aspect attributes in AspectWeaver.
    /// Developers should inherit from this class to create custom aspects that apply cross-cutting concerns.
    /// </summary>
    // Attribute Usage Configuration:
    // Method: Targets only methods for the MVP.
    // Inherited = true: Allows aspects defined on interfaces/base classes to be applied to implementations/overrides.
    // AllowMultiple = true: Allows applying multiple instances of the same aspect.
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public abstract class AspectAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the order in which this aspect is executed relative to other aspects applied to the same method.
        /// Aspects with a lower Order value are executed first in the "Before" phase
        /// and last in the "After/Finally" phase (LIFO stack behavior).
        /// The default value is 0.
        /// </summary>
        /// <remarks>
        /// Example execution sequence if [Cache(Order=10)] and [Log(Order=20)] are applied:
        /// 1. Cache (Entry)
        /// 2. Log (Entry)
        /// 3. Target method (or next aspect)
        /// 4. Log (Exit)
        /// 5. Cache (Exit)
        /// </remarks>
        public int Order { get; set; } = 0;
    }
}