// src/AspectWeaver.Generator/Emitters/PipelineEmitter.cs
using Aymen83.AspectWeaver.Generator.Analysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;

namespace Aymen83.AspectWeaver.Generator.Emitters
{
    internal static class PipelineEmitter
    {
        // (Constants remain the same)
        private const string FuncType = "global::System.Func";
        private const string ValueTaskType = "global::System.Threading.Tasks.ValueTask";
        private const string InvocationContextType = "global::Aymen83.AspectWeaver.Abstractions.InvocationContext";
        private const string DictionaryType = "global::System.Collections.Generic.Dictionary";
        private const string IServiceProviderType = "global::System.IServiceProvider";
        private const string AspectHandlerType = "global::Aymen83.AspectWeaver.Abstractions.IAspectHandler";
        private const string InvalidOperationExceptionType = "global::System.InvalidOperationException";
        private const string VoidResultFullName = "global::Aymen83.AspectWeaver.Abstractions.VoidResult";

        // (Variable names remain the same)
        private const string ContextVar = "__context";
        private const string PipelineVar = "__pipeline";
        private const string ServiceProviderVar = "__serviceProvider";
        // PBI 4.2: New variable name
        private const string MethodInfoVar = "__methodInfo";

        public static void EmitPipeline(IndentedWriter writer, InterceptionTarget target, MethodSignature signature)
        {
            var delegateType = $"{FuncType}<{InvocationContextType}, {ValueTaskType}<{signature.LogicalResultType}>>";

            // 1. Resolve IServiceProvider
            EmitServiceProviderResolution(writer, target);

            // 2. Create InvocationContext (Updated for PBI 4.2)
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

            var accessExpression = target.ProviderAccessExpression;

            writer.WriteLine($"{IServiceProviderType} {ServiceProviderVar} = {accessExpression};");

            // Runtime Safety Check
            var exceptionMessage = SymbolDisplay.FormatLiteral(
                $"The IServiceProvider accessed via '{accessExpression}' returned null. Ensure the provider is correctly initialized on the instance.", true);

            writer.WriteLine($"if ({ServiceProviderVar} == null) throw new {InvalidOperationExceptionType}({exceptionMessage});");
            writer.WriteLine();
        }
        #endregion

        #region Step 2: Invocation Context (PBI 4.2 Implementation)
        private static void EmitInvocationContext(IndentedWriter writer, InterceptionTarget target, string targetInstanceExpression)
        {
            var method = target.TargetMethod;
            writer.WriteLine($"// 2. Create InvocationContext");

            // 2.1. PBI 4.2: Retrieve MethodInfo
            EmitMethodInfoRetrieval(writer, target);

            // 2.2. Pack Arguments
            writer.WriteLine($"var __arguments = new {DictionaryType}<string, object?>()");
            writer.OpenBlock();
            foreach (var param in method.Parameters)
            {
                // Use positional arguments for compatibility.
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
            // PBI 4.2: Pass the retrieved MethodInfo
            writer.WriteLine($"methodInfo: {MethodInfoVar},");
            writer.WriteLine($"methodName: {methodNameLiteral},");
            writer.WriteLine($"targetTypeName: {typeNameLiteral},");
            writer.WriteLine($"arguments: __arguments");
            writer.Outdent();
            writer.WriteLine(");");
            writer.WriteLine();
        }

        // PBI 4.2: Helper to generate the MethodInfo retrieval logic using Type.GetMethod().
        private static void EmitMethodInfoRetrieval(IndentedWriter writer, InterceptionTarget target)
        {
            writer.WriteLine("// PBI 4.2: Resolve MethodInfo (Using Type.GetMethod for robustness).");

            var method = target.TargetMethod;

            // 1. Get the Type object of the containing type.
            var containingTypeFQN = method.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Included));
            writer.WriteLine($"var __targetType = typeof({containingTypeFQN});");

            // 2. Define the parameter types array for the specific overload.
            writer.WriteLine("var __paramTypes = new global::System.Type[]");
            writer.OpenBlock();
            foreach (var p in method.Parameters)
            {
                var paramTypeFQN = p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Included));
                // Handle ref/out parameters correctly for reflection lookup.
                if (p.RefKind != RefKind.None)
                {
                    // typeof(T).MakeByRefType() is required for matching ref/out parameters.
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
            // Define binding flags (Public/NonPublic, Instance/Static).
            string bindingFlags = "global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.NonPublic | ";
            bindingFlags += method.IsStatic ? "global::System.Reflection.BindingFlags.Static" : "global::System.Reflection.BindingFlags.Instance";

            // Handle Generic Methods: If generic, we first get the generic definition, then specialize it.
            if (method.IsGenericMethod)
            {
                // GetMethod on a generic type returns the Generic Method Definition when called with the correct parameter types.
                writer.WriteLine($"var __genericMethodDefinition = __targetType.GetMethod({methodNameLiteral}, {bindingFlags}, null, __paramTypes, null);");

                // Safety check for the definition.
                var exceptionMsgDef = SymbolDisplay.FormatLiteral($"Could not resolve Generic Method Definition for {method.Name}. This indicates an issue in AspectWeaver.", true);
                writer.WriteLine($"if (__genericMethodDefinition == null) throw new {InvalidOperationExceptionType}({exceptionMsgDef});");


                // Generate the specialization types array (TypeArguments used at the call site).
                writer.WriteLine("var __genericArgs = new global::System.Type[]");
                writer.OpenBlock();
                foreach (var typeArg in method.TypeArguments)
                {
                    var typeArgFQN = typeArg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Included));
                    writer.WriteLine($"typeof({typeArgFQN}),");
                }
                writer.CloseBlock(";");

                // Specialize the method.
                writer.WriteLine($"var {MethodInfoVar} = __genericMethodDefinition.MakeGenericMethod(__genericArgs);");
            }
            else
            {
                // Non-generic method call.
                writer.WriteLine($"var {MethodInfoVar} = __targetType.GetMethod({methodNameLiteral}, {bindingFlags}, null, __paramTypes, null);");

                // Safety check (should not happen if generation is correct).
                var exceptionMsg = SymbolDisplay.FormatLiteral($"Could not resolve MethodInfo for {method.Name}. This indicates an issue in AspectWeaver.", true);
                writer.WriteLine($"if ({MethodInfoVar} == null) throw new {InvalidOperationExceptionType}({exceptionMsg});");
            }
        }

        #endregion

        #region Step 3: Core Delegate
        // (Implementation remains the same as PBI 2.6)
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

        #region Step 4: Aspect Chain (Includes User Fix)
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

            // USER FIX APPLIED: Use nullable cast (?) to satisfy CS8600 in strict mode.
            writer.WriteLine($"var {handlerVar} = ({handlerInterfaceType}?){ServiceProviderVar}.GetService(typeof({handlerInterfaceType}));");

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
        // (Implementation remains the same)
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
        // (Implementation remains the same as PBI 2.6)
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