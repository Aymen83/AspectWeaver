using AspectWeaver.Generator.Analysis;
using Microsoft.CodeAnalysis;
using System.Linq;

namespace AspectWeaver.Generator.Emitters
{
    internal static class PipelineEmitter
    {
        // Define fully qualified names (FQNs) for types used in generated code.
        private const string FuncType = "global::System.Func";
        private const string ValueTaskType = "global::System.Threading.Tasks.ValueTask";
        private const string InvocationContextType = "global::AspectWeaver.Abstractions.InvocationContext";
        private const string DictionaryType = "global::System.Collections.Generic.Dictionary";
        private const string IServiceProviderType = "global::System.IServiceProvider";
        private const string AspectHandlerType = "global::AspectWeaver.Abstractions.IAspectHandler";
        private const string InvalidOperationExceptionType = "global::System.InvalidOperationException";


        // Variable names used in generated code.
        private const string ContextVar = "__context";
        private const string PipelineVar = "__pipeline";
        private const string ServiceProviderVar = "__serviceProvider";

        public static void EmitPipeline(IndentedWriter writer, InterceptionTarget target, MethodSignature signature)
        {
            // PBI 2.5 Constraint: Handle synchronous methods.
            if (signature.IsAsync)
            {
                // Fallback to passthrough for async methods until PBI 2.6.
                writer.WriteLine("// Async method detected. Falling back to passthrough (PBI 2.6).");
                EmitPassthroughBody(writer, target.TargetMethod, signature);
                return;
            }

            // Define the delegate type: Func<InvocationContext, ValueTask<TResult>>
            var delegateType = $"{FuncType}<{InvocationContextType}, {ValueTaskType}<{signature.LogicalResultType}>>";

            // 1. Resolve IServiceProvider (Using the injected placeholder)
            EmitServiceProviderResolution(writer);

            // 2. Create InvocationContext
            string targetInstanceExpression = signature.IsInstanceMethod ? MethodSignature.InstanceParameterName : "null";
            EmitInvocationContext(writer, target, targetInstanceExpression);

            // 3. Define the Core Delegate (The original method call)
            EmitCoreDelegate(writer, target, signature, delegateType);

            // 4. Build the Aspect Chain (Wrapping the delegate)
            EmitAspectChain(writer, target, signature);

            // 5. Execute the Pipeline (Synchronous)
            EmitPipelineExecution(writer, signature);
        }

        private static void EmitServiceProviderResolution(IndentedWriter writer)
        {
            // We use the PlaceholderServiceProvider injected during initialization.
            // This allows the code to compile and correctly simulates DI behavior.
            writer.WriteLine($"// 1. Resolve IServiceProvider (Placeholder for Epic 3)");
            writer.WriteLine($"{IServiceProviderType} {ServiceProviderVar} = new global::AspectWeaver.Generated.PlaceholderServiceProvider();");
            writer.WriteLine();
        }

        private static void EmitInvocationContext(IndentedWriter writer, InterceptionTarget target, string targetInstanceExpression)
        {
            var method = target.TargetMethod;
            writer.WriteLine($"// 2. Create InvocationContext");

            // 2.1. Pack Arguments
            writer.WriteLine($"var __arguments = new {DictionaryType}<string, object?>()");
            writer.OpenBlock();
            foreach (var param in method.Parameters)
            {
                // Note: Boxing occurs here (MVP technical debt).
                writer.WriteLine($"{{ \"{param.Name}\", {param.Name} }},");
            }
            writer.CloseBlock(suffix: ";");

            // 2.2. Create Context
            var typeName = method.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Included));

            writer.WriteLine($"var {ContextVar} = new {InvocationContextType}(");
            writer.Indent();
            writer.WriteLine($"targetInstance: {targetInstanceExpression},");
            writer.WriteLine($"serviceProvider: {ServiceProviderVar},");
            // Use C# 11 Raw String Literals for safety (requires PolySharp).
            writer.WriteLine($"methodName: \"\"\"{method.Name}\"\"\",");
            writer.WriteLine($"targetTypeName: \"\"\"{typeName}\"\"\",");
            writer.WriteLine($"arguments: __arguments");
            writer.Outdent();
            writer.WriteLine(");");
            writer.WriteLine();
        }

        private static void EmitCoreDelegate(IndentedWriter writer, InterceptionTarget target, MethodSignature signature, string delegateType)
        {
            writer.WriteLine($"// 3. Core: The original method call.");
            writer.WriteLine($"{delegateType} {PipelineVar} = (ctx) =>");
            writer.OpenBlock();

            var method = target.TargetMethod;

            // Determine the target of the call.
            string callTarget;
            if (signature.IsInstanceMethod)
            {
                // Use the original instance parameter for the MVP.
                callTarget = MethodSignature.InstanceParameterName;
            }
            else
            {
                callTarget = method.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Included));
            }

            // Construct the call expression.
            string callExpression = $"{callTarget}.{method.Name}{signature.GenericTypeParameters}({signature.Arguments})";

            if (signature.ReturnsVoid)
            {
                writer.WriteLine(callExpression + ";");
                // Return the placeholder VoidResult wrapped in a ValueTask.
                writer.WriteLine($"return new {ValueTaskType}<{signature.LogicalResultType}>({signature.LogicalResultType}.Instance);");
            }
            else
            {
                // Return the result wrapped in a ValueTask.
                writer.WriteLine($"var result = {callExpression};");
                writer.WriteLine($"return new {ValueTaskType}<{signature.LogicalResultType}>(result);");
            }

            writer.CloseBlock(suffix: ";");
            writer.WriteLine();
        }

        private static void EmitAspectChain(IndentedWriter writer, InterceptionTarget target, MethodSignature signature)
        {
            writer.WriteLine("// 4. Wrapping: Apply aspects (from inner to outer).");

            // Iterate backwards: The list is sorted by Order (ascending), so reversing ensures the lowest Order wraps the others (LIFO).
            int index = target.AppliedAspects.Length;
            foreach (var aspect in target.AppliedAspects.Reverse())
            {
                index--;
                EmitAspectWrapper(writer, aspect, signature, index);
            }
        }

        private static void EmitAspectWrapper(IndentedWriter writer, AspectInfo aspect, MethodSignature signature, int index)
        {
            var attributeType = aspect.AttributeData.AttributeClass!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Included));
            var handlerInterfaceType = $"{AspectHandlerType}<{attributeType}>";

            writer.WriteLine($"// Aspect {index}: {attributeType} (Order={aspect.Order})");

            // A. Capture the current pipeline in a local variable for the closure.
            var nextVar = $"__next{index}";
            writer.WriteLine($"var {nextVar} = {PipelineVar};");

            // B. Resolve the Handler using GetService.
            var handlerVar = $"__handler{index}";
            // We explicitly use GetService on the provider. The PlaceholderServiceProvider returns null.
            // We must check for null and throw, simulating a missing DI registration.
            writer.WriteLine($"var {handlerVar} = ({handlerInterfaceType}){ServiceProviderVar}.GetService(typeof({handlerInterfaceType}));");
            // Use Raw String Literal for the exception message.
            writer.WriteLine($"if ({handlerVar} == null) throw new {InvalidOperationExceptionType}(\"\"\"Handler not registered for aspect: {attributeType}\"\"\");");


            // C. Rehydrate the Attribute instance.
            var attributeVar = $"__attribute{index}";
            EmitAttributeRehydration(writer, attributeVar, attributeType, aspect.AttributeData);

            // D. Wrap the pipeline
            writer.WriteLine($"{PipelineVar} = (ctx) =>");
            writer.OpenBlock();
            // Call InterceptAsync<TResult>(attribute, context, next)
            writer.WriteLine($"return {handlerVar}.InterceptAsync<{signature.LogicalResultType}>({attributeVar}, ctx, {nextVar});");
            writer.CloseBlock(suffix: ";");
            writer.WriteLine();
        }

        #region Attribute Rehydration
        private static void EmitAttributeRehydration(IndentedWriter writer, string varName, string attributeType, AttributeData attributeData)
        {
            // Generate the 'new AttributeType(...)' expression using the values stored in AttributeData.

            // 1. Constructor Arguments
            var constructorArgs = string.Join(", ", attributeData.ConstructorArguments.Select(arg => TypedConstantToString(arg)));

            // 2. Named Arguments (Property Setters)
            var namedArgs = string.Join(", ", attributeData.NamedArguments.Select(kvp => $"{kvp.Key} = {TypedConstantToString(kvp.Value)}"));

            writer.Write($"var {varName} = new {attributeType}({constructorArgs})");

            if (!string.IsNullOrEmpty(namedArgs))
            {
                // Use object initializer syntax
                writer.WriteLine();
                writer.OpenBlock();
                writer.WriteLine(namedArgs);
                writer.CloseBlock(";");
            }
            else
            {
                writer.WriteLine(";");
            }
        }

        // Helper to convert Roslyn TypedConstant to a C# literal string.
        private static string TypedConstantToString(TypedConstant constant)
        {
            if (constant.IsNull) return "null";

            if (constant.Kind == TypedConstantKind.Primitive)
            {
                // Use C# 11 Raw String Literal for safety and cleanliness (requires PolySharp).
                if (constant.Value is string s) return $"\"\"\"{s}\"\"\"";
                if (constant.Value is bool b) return b ? "true" : "false";
                if (constant.Value is char c) return $"'{c}'";
                // Ensure numeric types are formatted correctly (culture-invariant).
                return global::System.Convert.ToString(constant.Value, global::System.Globalization.CultureInfo.InvariantCulture);
            }

            if (constant.Kind == TypedConstantKind.Enum)
            {
                var enumType = constant.Type!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Included));
                return $"({enumType})({constant.Value})";
            }

            if (constant.Kind == TypedConstantKind.Type && constant.Value is ITypeSymbol typeSymbol)
            {
                return $"typeof({typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Included))})";
            }

            // Handle Arrays (Complex, deferred for MVP)
            // TODO: Implement array initialization generation.
            return "default"; // Fallback for unsupported types
        }
        #endregion

        private static void EmitPipelineExecution(IndentedWriter writer, MethodSignature signature)
        {
            writer.WriteLine("// 5. Execute the pipeline (Synchronous).");
            // Invoke the pipeline and synchronously wait for the result.
            // .GetAwaiter().GetResult() is acceptable here because the underlying method is synchronous.
            writer.WriteLine($"var __finalResult = {PipelineVar}({ContextVar}).GetAwaiter().GetResult();");

            if (!signature.ReturnsVoid)
            {
                writer.WriteLine("return __finalResult;");
            }
            // If void, we discard the VoidResult and return implicitly.
        }

        // Helper method to reuse the passthrough logic (used as fallback for Async in PBI 2.5)
        internal static void EmitPassthroughBody(IndentedWriter writer, IMethodSymbol method, MethodSignature signature)
        {
            string callTarget;
            if (signature.IsInstanceMethod)
            {
                callTarget = MethodSignature.InstanceParameterName;
            }
            else
            {
                callTarget = method.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Included));
            }

            string callExpression = $"{callTarget}.{method.Name}{signature.GenericTypeParameters}({signature.Arguments});";

            // PBI 2.6: Add 'await' here if signature.IsAsync.
            if (method.ReturnsVoid)
            {
                writer.WriteLine(callExpression);
            }
            else
            {
                writer.WriteLine($"return {callExpression}");
            }
        }
    }
}