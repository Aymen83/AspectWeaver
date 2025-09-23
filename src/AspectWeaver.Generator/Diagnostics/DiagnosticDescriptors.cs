using Microsoft.CodeAnalysis;

namespace AspectWeaver.Generator.Diagnostics
{
    /// <summary>
    /// Contains the definitions for diagnostics (Errors and Warnings) produced by the AspectWeaver generator.
    /// </summary>
    internal static class DiagnosticDescriptors
    {
        // Category for DI related issues.
        private const string CategoryDI = "AspectWeaver.DI";

        /// <summary>
        /// AW001: Error when an aspect requires DI but no IServiceProvider can be accessed on the instance.
        /// </summary>
        public static readonly DiagnosticDescriptor AW001_ServiceProviderNotFound = new(
            id: "AW001",
            title: "IServiceProvider access required for aspect weaving",
            // FIX RS1032: Ensure the message is a single line and properly formatted with a period.
            messageFormat: "Method '{0}' uses aspects requiring dependency injection, but no accessible IServiceProvider was found on the containing type '{1}'. Ensure an 'internal' or 'public' field/property of type 'System.IServiceProvider' exists (e.g., 'internal IServiceProvider ServiceProvider {{ get; }}').",
            category: CategoryDI,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Aspect handlers require resolution via IServiceProvider. The generator must be able to access the provider from the intercepted instance."
        );

        /// <summary>
        /// AW002: Error when aspects are applied to static methods.
        /// </summary>
        public static readonly DiagnosticDescriptor AW002_StaticMethodNotSupported = new(
            id: "AW002",
            title: "Aspects requiring DI are not supported on static methods",
            // FIX RS1032: Ensure the message is a single line and properly formatted with a period.
            messageFormat: "Static method '{0}' cannot use aspects requiring dependency injection because there is no instance to provide the IServiceProvider",
            category: CategoryDI,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "Aspect weaving with dependency injection relies on accessing IServiceProvider from the target instance, which is unavailable for static methods."
        );
    }
}