using System;

namespace AspectWeaver.Abstractions
{
    /// <summary>
    /// The base class for all attributes that define an aspect.
    /// Attributes deriving from this class trigger the AspectWeaver source generator
    /// to weave the corresponding handler logic around the annotated method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
    public abstract class AspectAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the execution order of the aspect.
        /// Aspects with lower values are executed earlier (wrap around aspects with higher values).
        /// </summary>
        /// <remarks>
        /// The default order should typically be defined using a public const field named 'DefaultOrder'
        /// on the derived attribute class for optimal generator performance.
        /// </remarks>
        public int Order { get; set; } = 0;
    }
}