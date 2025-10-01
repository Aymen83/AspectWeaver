using Aymen83.AspectWeaver.Generator.Analysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using System.Linq;

namespace Aymen83.AspectWeaver.Generator.Emitters
{
    /// <summary>
    /// Emits the C# source code for the interceptor methods.
    /// </summary>
    internal static class InterceptorEmitter
    {
        private const string GeneratedNamespace = "Aymen83.AspectWeaver.Generated";
        private const string GeneratedClassName = "Interceptors";
        private const string CacheSuffix = "_Cache";
        internal const string CachedMethodInfoFieldName = "MethodInfo";
        private const string InitMethodName = "InitMethodInfo";
        private const string InterceptMethodPrefix = "InterceptMethod";
        internal const string AttributePrefix = "Attribute_";
        private const string InitAttributePrefix = "InitAttribute_";

        public static string Emit(ImmutableArray<InterceptionTarget> targets)
        {
            var distinctTargets = targets.Distinct(InterceptionTargetComparer.Instance).ToList();

            if (distinctTargets.Count == 0)
            {
                return string.Empty;
            }

            using var writer = new IndentedWriter();
            writer.WriteFileHeader();

            // System.Runtime.CompilerServices is required for [InterceptsLocation] and [MethodImpl].
            writer.WriteLine("using System.Runtime.CompilerServices;");
            // System.Diagnostics is required for Debugger attributes.
            writer.WriteLine("using System.Diagnostics;");
            // Optimization: Required for reflection types (MethodInfo, BindingFlags).
            writer.WriteLine("using System.Reflection;");
            writer.WriteLine();

            writer.WriteLine($"namespace {GeneratedNamespace}");
            writer.OpenBlock();

            EmitDebuggerAttributes(writer);
            writer.WriteLine($"internal static class {GeneratedClassName}");
            writer.OpenBlock();

            int counter = 0;
            foreach (var target in distinctTargets)
            {
                if (counter > 0) writer.WriteLine();

                // 1. Emit the Interceptor Method (must be in the top-level class).
                var cacheClassName = $"Interceptor{counter}{CacheSuffix}";
                var interceptorName = $"{InterceptMethodPrefix}{counter}";
                EmitInterceptorMethod(writer, target, interceptorName, cacheClassName);
                writer.WriteLine();

                // 2. Emit the nested Caching Class definition.
                EmitCachingClass(writer, target, cacheClassName);

                counter++;
            }

            writer.CloseBlock(); // Close class
            writer.CloseBlock(); // Close namespace

            return writer.ToString();
        }

        /// <summary>
        /// Emits debugger attributes to instruct the debugger to step over this generated code,
        /// improving the debugging experience. Uses fully qualified names for robustness.
        /// </summary>
        private static void EmitDebuggerAttributes(IndentedWriter writer)
        {
            // We use FQNs for robustness.
            writer.WriteLine("[global::System.Diagnostics.DebuggerStepThrough]");
            writer.WriteLine("[global::System.Diagnostics.DebuggerNonUserCode]");
        }

        private static void EmitCachingClass(IndentedWriter writer, InterceptionTarget target, string cacheClassName)
        {
            // This nested private class caches reflection results for a single interceptor.
            // This pattern avoids cluttering the parent class and ensures thread-safe, lazy initialization.
            EmitDebuggerAttributes(writer);
            writer.WriteLine($"private static class {cacheClassName}");
            writer.OpenBlock();

            // 1. Generate the Argument Struct definition.
            // It must be defined before being used by the interceptor method logic (via PipelineEmitter).
            ArgumentStructEmitter.Emit(writer, target.TargetMethod);
            writer.WriteLine();

            // 2. The cached static field, initialized via the InitMethodInfo method.
            EmitCachedMethodInfoField(writer);

            // 3. Attribute caching
            EmitAttributeCache(writer, target);

            // 4. The initialization method (runs once per type).
            EmitInitializationMethod(writer, target);

            writer.CloseBlock();
        }

        // generate attribute cache fields and initialization methods.
        private static void EmitAttributeCache(IndentedWriter writer, InterceptionTarget target)
        {
            // We iterate in reverse order because the pipeline wraps from inner to outer (LIFO).
            // This ensures the indices match the pipeline generation logic in PipelineEmitter.
            int index = target.AppliedAspects.Length;
            foreach (var aspect in target.AppliedAspects.Reverse())
            {
                index--;
                var attributeTypeFQN = aspect.AttributeData.AttributeClass!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Included));
                var fieldName = $"{AttributePrefix}{index}";
                var initMethodName = $"{InitAttributePrefix}{index}";

                // 1. Emit the cached field: internal static readonly AttributeType Attribute_X = InitAttribute_X();
                writer.WriteLine($"internal static readonly {attributeTypeFQN} {fieldName} = {initMethodName}();");
                writer.WriteLine();

                // 2. Emit the initialization method.
                EmitAttributeInitializationMethod(writer, aspect.AttributeData, attributeTypeFQN, initMethodName);
            }
        }

        // generate the static initialization logic for an attribute.
        private static void EmitAttributeInitializationMethod(IndentedWriter writer, AttributeData attributeData, string attributeTypeFQN, string initMethodName)
        {
            writer.WriteLine($"private static {attributeTypeFQN} {initMethodName}()");
            writer.OpenBlock();

            // Use the centralized AttributeEmitter to generate the instantiation code.
            var instantiationExpression = AttributeEmitter.GenerateAttributeInstantiation(attributeData);
            writer.WriteLine($"return {instantiationExpression};");

            writer.CloseBlock();
            writer.WriteLine();
        }

        private static void EmitCachedMethodInfoField(IndentedWriter writer)
        {
            // This static readonly field holds the MethodInfo, initialized by InitMethodInfo().
            // Initialization is thread-safe and lazy due to the nature of static constructors.
            writer.WriteLine($"internal static readonly MethodInfo {CachedMethodInfoFieldName} = {InitMethodName}();");
            writer.WriteLine();
        }

        // This method uses reflection to get the MethodInfo for the target method.
        // This logic was moved from PipelineEmitter to be self-contained within this caching class.
        private static void EmitInitializationMethod(IndentedWriter writer, InterceptionTarget target)
        {
            // We use FQNs for robustness within the generated initialization method.
            writer.WriteLine($"private static MethodInfo {InitMethodName}()");
            writer.OpenBlock();

            var method = target.TargetMethod;
            var invalidOperationExceptionType = "global::System.InvalidOperationException";

            // 1. Get the Type object.
            var containingTypeFQN = method.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Included));
            writer.WriteLine($"var __targetType = typeof({containingTypeFQN});");

            // 2. Define the parameter types array.
            writer.WriteLine("var __paramTypes = new global::System.Type[]");
            writer.OpenBlock();
            foreach (var p in method.Parameters)
            {
                var paramTypeFQN = p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Included));
                if (p.RefKind != RefKind.None)
                {
                    writer.WriteLine($"typeof({paramTypeFQN}).MakeByRefType(),");
                }
                else
                {
                    writer.WriteLine($"typeof({paramTypeFQN}),");
                }
            }
            writer.CloseBlock(";");

            // 3. Call GetMethod().
            var methodNameLiteral = SymbolDisplay.FormatLiteral(method.Name, true);
            // Use the fully qualified names for BindingFlags within the generated code.
            string bindingFlags = "BindingFlags.Public | BindingFlags.NonPublic | ";
            bindingFlags += method.IsStatic ? "BindingFlags.Static" : "BindingFlags.Instance";

            if (method.IsGenericMethod)
            {
                writer.WriteLine($"var __genericMethodDefinition = __targetType.GetMethod({methodNameLiteral}, {bindingFlags}, null, __paramTypes, null);");

                // Safety check during initialization.
                var exceptionMsgDef = SymbolDisplay.FormatLiteral($"Could not resolve Generic Method Definition for {method.Name} during initialization.", true);
                writer.WriteLine($"if (__genericMethodDefinition == null) throw new {invalidOperationExceptionType}({exceptionMsgDef});");

                writer.WriteLine("var __genericArgs = new global::System.Type[]");
                writer.OpenBlock();
                foreach (var typeArg in method.TypeArguments)
                {
                    var typeArgFQN = typeArg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Included));
                    writer.WriteLine($"typeof({typeArgFQN}),");
                }
                writer.CloseBlock(";");

                writer.WriteLine($"return __genericMethodDefinition.MakeGenericMethod(__genericArgs);");
            }
            else
            {
                writer.WriteLine($"var methodInfo = __targetType.GetMethod({methodNameLiteral}, {bindingFlags}, null, __paramTypes, null);");

                // Safety check during initialization.
                var exceptionMsg = SymbolDisplay.FormatLiteral($"Could not resolve MethodInfo for {method.Name} during initialization.", true);
                writer.WriteLine($"if (methodInfo == null) throw new {invalidOperationExceptionType}({exceptionMsg});");
                writer.WriteLine("return methodInfo;");
            }

            writer.CloseBlock();
            writer.WriteLine();
        }

        private static void EmitInterceptorMethod(IndentedWriter writer, InterceptionTarget target, string interceptorName, string cacheClassName)
        {
            var signature = new MethodSignature(target.TargetMethod);

            // 1. Emit [InterceptsLocation] attribute
            EmitInterceptsLocationAttribute(writer, target.Location);

            // 2. Emit Performance Optimization Attribute
            EmitPerformanceAttributes(writer);

            // 3. Emit the method signature
            string asyncModifier = signature.IsAsync ? "async " : "";

            writer.Write($"internal static {asyncModifier}{signature.ReturnType} {interceptorName}{signature.GenericTypeParameters}(");
            writer.Write(signature.Parameters);
            writer.Write(")");

            // Append constraints directly.
            writer.Write(signature.GenericConstraints);

            // Ensure the line ends before opening the block.
            writer.WriteLine();

            // 4. Emit the method body
            writer.OpenBlock();
            PipelineEmitter.EmitPipeline(writer, target, signature, cacheClassName);
            writer.CloseBlock();
        }

        private static void EmitInterceptsLocationAttribute(IndentedWriter writer, InterceptableLocation location)
        {
            writer.WriteLine($"[global::System.Runtime.CompilerServices.InterceptsLocation(version: {location.Version}, data: \"{location.Data}\")]");
        }

        private static void EmitPerformanceAttributes(IndentedWriter writer)
        {
            writer.WriteLine("[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
        }
    }
}