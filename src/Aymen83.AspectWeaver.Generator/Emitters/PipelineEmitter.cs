using Aymen83.AspectWeaver.Generator.Analysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;

namespace Aymen83.AspectWeaver.Generator.Emitters
{
    /// <summary>
    /// Emits the aspect pipeline for an intercepted method.
    /// </summary>
    internal static class PipelineEmitter
    {
        private const string FuncType = "global::System.Func";
        private const string ValueTaskType = "global::System.Threading.Tasks.ValueTask";
        private const string InvocationContextType = "global::Aymen83.AspectWeaver.Abstractions.InvocationContext";
        private const string DictionaryType = "global::System.Collections.Generic.Dictionary";
        private const string IServiceProviderType = "global::System.IServiceProvider";
        private const string AspectHandlerType = "global::Aymen83.AspectWeaver.Abstractions.IAspectHandler";
        private const string InvalidOperationExceptionType = "global::System.InvalidOperationException";
        private const string VoidResultFullName = "global::Aymen83.AspectWeaver.Abstractions.VoidResult";

        private const string ContextVar = "__context";
        private const string PipelineVar = "__pipeline";
        private const string ServiceProviderVar = "__serviceProvider";

        public static void EmitPipeline(IndentedWriter writer, InterceptionTarget target, MethodSignature signature, string cacheClassName)
        {
            var delegateType = $"{FuncType}<{InvocationContextType}, {ValueTaskType}<{signature.LogicalResultType}>>";

            // 1. Resolve IServiceProvider
            EmitServiceProviderResolution(writer, target);

            // Construct the access expression for the cached MethodInfo.
            var cachedMethodInfoAccessExpression = $"{cacheClassName}.{InterceptorEmitter.CachedMethodInfoFieldName}";

            // 2. Create InvocationContext
            string targetInstanceExpression = signature.IsInstanceMethod ? MethodSignature.InstanceParameterName : "null";
            EmitInvocationContext(writer, target, targetInstanceExpression, cachedMethodInfoAccessExpression);

            // 3. Define the Core Delegate
            EmitCoreDelegate(writer, target, signature, delegateType);

            // 4. Build the Aspect Chain
            EmitAspectChain(writer, target, signature, cacheClassName);

            // 5. Execute the Pipeline
            EmitPipelineExecution(writer, signature);
        }

        #region Step 1: Service Provider Resolution
        private static void EmitServiceProviderResolution(IndentedWriter writer, InterceptionTarget target)
        {
            writer.WriteLine($"// 1. Resolve IServiceProvider");

            var accessExpression = target.ProviderAccessExpression;

            writer.WriteLine($"{IServiceProviderType} {ServiceProviderVar} = {accessExpression};");

            // Runtime Safety Check
            var exceptionMessage = SymbolDisplay.FormatLiteral(
                $"The IServiceProvider accessed via '{accessExpression}' returned null. Ensure the provider is correctly initialized on the instance.", true);

            writer.WriteLine($"if ({ServiceProviderVar} == null) throw new {InvalidOperationExceptionType}({exceptionMessage});");
            writer.WriteLine();
        }
        #endregion

        #region Step 2: Invocation Context
        private static void EmitInvocationContext(IndentedWriter writer, InterceptionTarget target, string targetInstanceExpression, string cachedMethodInfoAccessExpression)
        {
            var method = target.TargetMethod;
            writer.WriteLine($"// 2. Create InvocationContext");

            // 2.2. Pack Arguments
            writer.WriteLine($"var __arguments = new {DictionaryType}<string, object?>()");
            writer.OpenBlock();
            foreach (var param in method.Parameters)
            {
                var paramNameLiteral = SymbolDisplay.FormatLiteral(param.Name, true);
                writer.WriteLine($"{{ {paramNameLiteral}, {param.Name} }},");
            }
            writer.CloseBlock(suffix: ";");

            // 2.3. Create Context
            var typeName = method.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Included));

            string methodNameLiteral = SymbolDisplay.FormatLiteral(method.Name, true);
            string typeNameLiteral = SymbolDisplay.FormatLiteral(typeName, true);


            writer.WriteLine($"var {ContextVar} = new {InvocationContextType}(");
            writer.Indent();
            writer.WriteLine($"targetInstance: {targetInstanceExpression},");
            writer.WriteLine($"serviceProvider: {ServiceProviderVar},");
            writer.WriteLine($"methodInfo: {cachedMethodInfoAccessExpression},");
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
        private static void EmitAspectChain(IndentedWriter writer, InterceptionTarget target, MethodSignature signature, string cacheClassName)
        {
            writer.WriteLine("// 4. Wrapping: Apply aspects (from inner to outer).");

            int index = target.AppliedAspects.Length;
            foreach (var aspect in target.AppliedAspects.Reverse())
            {
                index--;
                EmitAspectWrapper(writer, aspect, signature, index, cacheClassName);
            }
        }

        private static void EmitAspectWrapper(IndentedWriter writer, AspectInfo aspect, MethodSignature signature, int index, string cacheClassName)
        {
            var attributeType = aspect.AttributeData.AttributeClass!.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Included));
            var handlerInterfaceType = $"{AspectHandlerType}<{attributeType}>";

            writer.WriteLine($"// Aspect {index}: {attributeType} (Order={aspect.Order})");

            var nextVar = $"__next{index}";
            writer.WriteLine($"var {nextVar} = {PipelineVar};");

            var handlerVar = $"__handler{index}";

            // Use nullable cast (?) to satisfy CS8600 in strict mode.
            writer.WriteLine($"var {handlerVar} = ({handlerInterfaceType}?){ServiceProviderVar}.GetService(typeof({handlerInterfaceType}));");

            var exceptionMessageLiteral = SymbolDisplay.FormatLiteral($"Handler not registered for aspect: {attributeType}", true);
            writer.WriteLine($"if ({handlerVar} == null) throw new {InvalidOperationExceptionType}({exceptionMessageLiteral});");


            // Access the cached attribute instance instead of rehydrating locally.
            // Example: var __attribute0 = Interceptor0_Cache.Attribute_0;
            var attributeVar = $"__attribute{index}";
            var cachedAttributeAccessExpression = $"{cacheClassName}.{InterceptorEmitter.AttributePrefix}{index}";
            writer.WriteLine($"var {attributeVar} = {cachedAttributeAccessExpression};");

            writer.WriteLine($"{PipelineVar} = (ctx) =>");
            writer.OpenBlock();
            writer.WriteLine($"return {handlerVar}.InterceptAsync<{signature.LogicalResultType}>({attributeVar}, ctx, {nextVar});");
            writer.CloseBlock(suffix: ";");
            writer.WriteLine();
        }
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