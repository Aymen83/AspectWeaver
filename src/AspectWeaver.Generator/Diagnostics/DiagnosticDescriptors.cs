using Microsoft.CodeAnalysis;

namespace AspectWeaver.Generator.Diagnostics
{
    internal static class DiagnosticDescriptors
    {
        // Categories
        private const string CategoryDI = "AspectWeaver.DI";
        // PBI 5.4: New Categories
        private const string CategoryUsage = "AspectWeaver.Usage";
        private const string CategoryConfiguration = "AspectWeaver.Configuration";

        // AW001, AW002 remain the same...
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

        // PBI 5.4: New Diagnostics

        /// <summary>
        /// AW003: Error when an AspectAttribute is applied to an invalid target (e.g., Property, Field).
        /// </summary>
        public static readonly DiagnosticDescriptor AW003_InvalidAspectTarget = new(
            id: "AW003",
            title: "AspectAttribute applied to an invalid target",
            // Ensure single line and period for RS1032 compliance.
            messageFormat: "Aspect '{0}' cannot be applied to '{1}'. AspectAttributes are only valid on methods.",
            category: CategoryUsage,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "AspectAttributes derived from AspectWeaver.Abstractions.AspectAttribute can only be applied to methods.",
            customTags: new[] { WellKnownDiagnosticTags.CompilationEnd }
        );

        // AW004 is reserved for PBI 5.5

        /// <summary>
        /// AW005: Error when an attribute configuration value is invalid.
        /// </summary>
        public static readonly DiagnosticDescriptor AW005_InvalidAttributeConfiguration = new(
            id: "AW005",
            title: "Invalid configuration value for attribute",
            // Ensure single line and period for RS1032 compliance.
            messageFormat: "Invalid configuration for '{0}': {1}",
            category: CategoryConfiguration,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "The configuration values provided for the attribute are outside the allowed range or invalid."
        );
    }
}