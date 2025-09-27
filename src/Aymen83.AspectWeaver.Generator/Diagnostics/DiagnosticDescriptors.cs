using Microsoft.CodeAnalysis;

namespace Aymen83.AspectWeaver.Generator.Diagnostics
{
    /// <summary>
    /// Defines the diagnostic descriptors used by the AspectWeaver generator and analyzers.
    /// </summary>
    internal static class DiagnosticDescriptors
    {
        private const string CategoryDI = "Aymen83.AspectWeaver.DI";
        private const string CategoryUsage = "Aymen83.AspectWeaver.Usage";
        private const string CategoryConfiguration = "Aymen83.AspectWeaver.Configuration";
        private const string CategoryLimitations = "Aymen83.AspectWeaver.Limitations";

        public static readonly DiagnosticDescriptor AW001_ServiceProviderNotFound = new(
           id: "AW001",
           title: "IServiceProvider access required for aspect weaving",
           messageFormat: "Method '{0}' uses aspects requiring dependency injection, but no accessible IServiceProvider was found on the containing type '{1}'. Ensure an 'internal' or 'public' field/property of type 'System.IServiceProvider' exists (e.g., 'internal IServiceProvider ServiceProvider {{ get; }}').",
           category: CategoryDI,
           defaultSeverity: DiagnosticSeverity.Error,
           isEnabledByDefault: true,
           description: "Aspect handlers require resolution via IServiceProvider. The generator must be able to access the provider from the intercepted instance."
       );

        public static readonly DiagnosticDescriptor AW002_StaticMethodNotSupported = new(
            id: "AW002",
            title: "Aspects requiring DI are not supported on static methods",
            messageFormat: "Static method '{0}' cannot use aspects requiring dependency injection because there is no instance to provide the IServiceProvider",
            category: CategoryDI,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Aspect weaving with dependency injection relies on accessing IServiceProvider from the target instance, which is unavailable for static methods."
        );

        public static readonly DiagnosticDescriptor AW003_InvalidAspectTarget = new(
             id: "AW003",
             title: "AspectAttribute applied to an invalid target",
             messageFormat: "Aspect '{0}' cannot be applied to '{1}'. AspectAttributes are only valid on methods.",
             category: CategoryUsage,
             defaultSeverity: DiagnosticSeverity.Error,
             isEnabledByDefault: true,
             description: "AspectAttributes derived from AspectWeaver.Abstractions.AspectAttribute can only be applied to methods.",
             customTags: [WellKnownDiagnosticTags.CompilationEnd]
         );

        /// <summary>
        /// AW004: Warning when an invocation pattern cannot be intercepted by C# 12 Interceptors.
        /// </summary>
        public static readonly DiagnosticDescriptor AW004_UninterceptableCallPattern = new(
            id: "AW004",
            title: "Method call cannot be intercepted due to language limitations",
            messageFormat: "The call to '{0}' cannot be intercepted. C# 12 Interceptors do not support calls using 'base.' access.",
            category: CategoryLimitations,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "C# 12 Interceptors have limitations on which call patterns can be redirected. Calls using 'base.' access are executed directly."
        );

        public static readonly DiagnosticDescriptor AW005_InvalidAttributeConfiguration = new(
           id: "AW005",
           title: "Invalid configuration value for attribute",
           messageFormat: "Invalid configuration for '{0}': {1}",
           category: CategoryConfiguration,
           defaultSeverity: DiagnosticSeverity.Error,
           isEnabledByDefault: true,
           description: "The configuration values provided for the attribute are outside the allowed range or invalid."
       );

        /// <summary>
        /// AW006: Error when aspects are applied to methods using ref struct parameters (e.g., Span<T>).
        /// </summary>
        public static readonly DiagnosticDescriptor AW006_RefStructNotSupported = new(
            id: "AW006",
            title: "Aspects are not supported on methods with ref struct parameters",
            messageFormat: "Method '{0}' cannot use aspects because it has a 'ref struct' parameter ('{1}'). Parameters like Span<T> or ReadOnlySpan<T> cannot be safely captured by the interception pipeline.",
            category: CategoryLimitations,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "The AspectWeaver pipeline requires capturing arguments, which is not safely possible with 'ref struct' types."
        );
    }
}