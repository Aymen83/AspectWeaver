// src/AspectWeaver.Generator/Emitters/PipelineEmitter.cs
using AspectWeaver.Generator.Analysis;
using Microsoft.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;

namespace AspectWeaver.Generator.Emitters
{
    internal static class PipelineEmitter
    {
        // (Constants remain the same)
        private const string FuncType = "global::System.Func";
        private const string ValueTaskType = "global::System.Threading.Tasks.ValueTask";
        private const string InvocationContextType = "global::AspectWeaver.Abstractions.InvocationContext";
        private const string DictionaryType = "global::System.Collections.Generic.Dictionary";
        private const string IServiceProviderType = "global::System.IServiceProvider";
        private const string AspectHandlerType = "global::AspectWeaver.Abstractions.IAspectHandler";
        private const string InvalidOperationExceptionType = "global::System.InvalidOperationException";
        private const string VoidResultFullName = "global::AspectWeaver.Abstractions.VoidResult";

        // (Variable names remain the same)
        private const string ContextVar = "__context";
        private const string PipelineVar = "__pipeline";
        private const string ServiceProviderVar = "__serviceProvider";

        public static void EmitPipeline(IndentedWriter writer, InterceptionTarget target, MethodSignature signature)
        {
            // Define the delegate type: Func<InvocationContext, ValueTask<TResult>>
            var delegateType = $"{FuncType}<{InvocationContextType}, {ValueTaskType}<{signature.LogicalResultType}>>";

            // 1. Resolve IServiceProvider (PBI 3.3: Updated implementation)
            EmitServiceProviderResolution(writer, target);

            // 2. Create InvocationContext
            string targetInstanceExpression = signature.IsInstanceMethod ? MethodSignature.InstanceParameterName : "null";
            EmitInvocationContext(writer, target, targetInstanceExpression);

            // 3. Define the Core Delegate
            EmitCoreDelegate(writer, target, signature, delegateType);

            // 4. Build the Aspect Chain
            EmitAspectChain(writer, target, signature);

            // 5. Execute the Pipeline
            EmitPipelineExecution(writer, signature);
        }

        #region Step 1: Service Provider Resolution (PBI 3.3 Implementation)
        private static void EmitServiceProviderResolution(IndentedWriter writer, InterceptionTarget target)
        {
            writer.WriteLine($"// 1. Resolve IServiceProvider");

            // Use the access expression determined during analysis (e.g., "__instance.ServiceProvider").
            // We know this expression is valid because the analysis phase (PBI 3.2) ensures it and filters out invalid targets (AW001/AW002).
            var accessExpression = target.ProviderAccessExpression;

            writer.WriteLine($"{IServiceProviderType} {ServiceProviderVar} = {accessExpression};");

            // Runtime Safety Check: Ensure the provider is not null.
            // This handles cases where the field/property exists but hasn't been initialized (e.g., constructor injection failure).
            // We use the compatible FormatLiteral signature (string, bool).
            var exceptionMessage = SymbolDisplay.FormatLiteral(
                $"The IServiceProvider accessed via '{accessExpression}' returned null. Ensure the provider is correctly initialized on the instance.", true);

            writer.WriteLine($"if ({ServiceProviderVar} == null) throw new {InvalidOperationExceptionType}({exceptionMessage});");
            writer.WriteLine();
        }
        #endregion

        // (All other methods remain the same as finalized in previous PBIs)

        #region Step 2: Invocation Context
        private static void EmitInvocationContext(IndentedWriter writer, InterceptionTarget target, string targetInstanceExpression)
        {
            var method = target.TargetMethod;
            writer.WriteLine($"// 2. Create InvocationContext");

            // 2.1. Pack Arguments
            writer.WriteLine($"var __arguments = new {DictionaryType}<string, object?>()");
            writer.OpenBlock();
            foreach (var param in method.Parameters)
            {
                var paramNameLiteral = SymbolDisplay.FormatLiteral(param.Name, true);
                writer.WriteLine($"{{ {paramNameLiteral}, {param.Name} }},");
            }
            writer.CloseBlock(suffix: ";");

            // 2.2. Create Context
            var typeName = method.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Included));

            string methodNameLiteral = SymbolDisplay.FormatLiteral(method.Name, true);
            string typeNameLiteral = SymbolDisplay.FormatLiteral(typeName, true);


            writer.WriteLine($"var {ContextVar} = new {InvocationContextType}(");
            writer.Indent();
            writer.WriteLine($"targetInstance: {targetInstanceExpression},");
            writer.WriteLine($"serviceProvider: {ServiceProviderVar},");
            // Use the robust literals
            writer.WriteLine($"methodName: {methodNameLiteral},");
            writer.WriteLine($"targetTypeName: {typeNameLiteral},");
            writer.WriteLine($"arguments: __arguments");
            writer.Outdent();
            writer.WriteLine(");");
            writer.WriteLine();
        }
        #endregion

        #region Step 3: Core Delegate
        private static void EmitCoreDelegate(IndentedWriter writer, InterceptionTarget target, MethodSignature signature, string delegateType)
        {
            writer.WriteLine($"// 3. Core: The original method call.");

            string asyncModifier = signature.IsAsync ? "async " : "";

            writer.WriteLine($"{delegateType} {PipelineVar} = {asyncModifier}(ctx) =>");
            writer.OpenBlock();

            var method = target.TargetMethod;

            string callTarget;
            if (signature.IsInstanceMethod)
            {
                callTarget = MethodSignature.InstanceParameterName;
            }
            else
            {
                callTarget = method.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Included));
            }

            string callExpression = $"{callTarget}.{method.Name}{signature.GenericTypeParameters}({signature.Arguments})";

            string awaitPrefix = signature.IsAsync ? "await " : "";
            string configureAwaitSuffix = signature.IsAsync ? ".ConfigureAwait(false)" : "";


            bool isVoidLogical = signature.LogicalResultType == VoidResultFullName;

            if (isVoidLogical)
            {
                writer.WriteLine(awaitPrefix + callExpression + configureAwaitSuffix + ";");

                if (signature.IsAsync)
                {
                    writer.WriteLine($"return {signature.LogicalResultType}.Instance;");
                }
                else
                {
                    writer.WriteLine($"return new {ValueTaskType}<{signature.LogicalResultType}>({signature.LogicalResultType}.Instance);");
                }
            }
            else
            {
                writer.WriteLine($"var result = {awaitPrefix}{callExpression}{configureAwaitSuffix};");

                if (signature.IsAsync)
                {
                    writer.WriteLine("return result;");
                }
                else
                {
                    writer.WriteLine($"return new {ValueTaskType}<{signature.LogicalResultType}>(result);");
                }
            }

            writer.CloseBlock(suffix: ";");
            writer.WriteLine();
        }
        #endregion

        #region Step 4: Aspect Chain
        private static void EmitAspectChain(IndentedWriter writer, InterceptionTarget target, MethodSignature signature)
        {
            writer.WriteLine("// 4. Wrapping: Apply aspects (from inner to outer).");

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

            var nextVar = $"__next{index}";
            writer.WriteLine($"var {nextVar} = {PipelineVar};");

            var handlerVar = $"__handler{index}";
            writer.WriteLine($"var {handlerVar} = ({handlerInterfaceType}){ServiceProviderVar}.GetService(typeof({handlerInterfaceType}));");

            // Use positional arguments for compatibility.
            var exceptionMessageLiteral = SymbolDisplay.FormatLiteral($"Handler not registered for aspect: {attributeType}", true);
            writer.WriteLine($"if ({handlerVar} == null) throw new {InvalidOperationExceptionType}({exceptionMessageLiteral});");


            var attributeVar = $"__attribute{index}";
            EmitAttributeRehydration(writer, attributeVar, attributeType, aspect.AttributeData);

            writer.WriteLine($"{PipelineVar} = (ctx) =>");
            writer.OpenBlock();
            writer.WriteLine($"return {handlerVar}.InterceptAsync<{signature.LogicalResultType}>({attributeVar}, ctx, {nextVar});");
            writer.CloseBlock(suffix: ";");
            writer.WriteLine();
        }

        #region Attribute Rehydration
        private static void EmitAttributeRehydration(IndentedWriter writer, string varName, string attributeType, AttributeData attributeData)
        {
            var constructorArgs = string.Join(", ", attributeData.ConstructorArguments.Select(arg => TypedConstantToString(arg)));
            var namedArgs = string.Join(", ", attributeData.NamedArguments.Select(kvp => $"{kvp.Key} = {TypedConstantToString(kvp.Value)}"));

            writer.Write($"var {varName} = new {attributeType}({constructorArgs})");

            if (!string.IsNullOrEmpty(namedArgs))
            {
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

        private static string TypedConstantToString(TypedConstant constant)
        {
            if (constant.IsNull) return "null";

            if (constant.Kind == TypedConstantKind.Primitive)
            {
                if (constant.Value is string s)
                {
                    return SymbolDisplay.FormatLiteral(s, true);
                }
                if (constant.Value is bool b) return b ? "true" : "false";
                if (constant.Value is char c)
                {
                    return SymbolDisplay.FormatLiteral(c, true);
                }

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

            return "default";
        }
        #endregion
        #endregion

        #region Step 5: Execution
        private static void EmitPipelineExecution(IndentedWriter writer, MethodSignature signature)
        {
            writer.WriteLine("// 5. Execute the pipeline.");

            if (signature.IsAsync)
            {
                EmitAsyncPipelineExecution(writer, signature);
            }
            else
            {
                writer.WriteLine("// Execution Mode: Synchronous.");
                writer.WriteLine($"var __finalResult = {PipelineVar}({ContextVar}).GetAwaiter().GetResult();");

                if (!signature.ReturnsVoid)
                {
                    writer.WriteLine("return __finalResult;");
                }
            }
        }

        private static void EmitAsyncPipelineExecution(IndentedWriter writer, MethodSignature signature)
        {
            writer.WriteLine("// Execution Mode: Asynchronous.");

            writer.WriteLine($"var __finalResult = await {PipelineVar}({ContextVar}).ConfigureAwait(false);");

            if (signature.LogicalResultType == VoidResultFullName)
            {
                return;
            }

            writer.WriteLine("return __finalResult;");
        }
        #endregion
    }
}