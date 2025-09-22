using AspectWeaver.Generator.Analysis;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Linq;

namespace AspectWeaver.Generator.Emitters
{
    internal static class InterceptorEmitter
    {
        private const string GeneratedNamespace = "AspectWeaver.Generated";
        private const string GeneratedClassName = "Interceptors";

        public static string Emit(ImmutableArray<InterceptionTarget> targets)
        {
            // We use Distinct() with the comparer as a safety measure against potential duplicates in the pipeline.
            var distinctTargets = targets.Distinct(InterceptionTargetComparer.Instance).ToList();

            if (distinctTargets.Count == 0)
            {
                return string.Empty;
            }

            using var writer = new IndentedWriter();
            writer.WriteFileHeader();

            // Usings required by the generated code.
            writer.WriteLine("using System.Runtime.CompilerServices;");
            writer.WriteLine();

            writer.WriteLine($"namespace {GeneratedNamespace}");
            writer.OpenBlock();

            // Interceptors must be hosted in a static class.
            writer.WriteLine($"internal static class {GeneratedClassName}");
            writer.OpenBlock();

            int counter = 0;
            foreach (var target in distinctTargets)
            {
                if (counter > 0) writer.WriteLine();

                // Generate a unique name for the interceptor method. C# 12 requires one method per location.
                var interceptorName = $"InterceptMethod{counter}";
                EmitInterceptorMethod(writer, target, interceptorName);
                counter++;
            }

            writer.CloseBlock(); // Close class
            writer.CloseBlock(); // Close namespace

            return writer.ToString();
        }

        private static void EmitInterceptorMethod(IndentedWriter writer, InterceptionTarget target, string interceptorName)
        {
            var method = target.TargetMethod;
            var signature = new MethodSignature(method);

            // 1. Emit [InterceptsLocation] attribute
            EmitInterceptsLocationAttribute(writer, target.Location);

            // 2. Emit the method signature
            writer.Write($"internal static {signature.ReturnType} {interceptorName}{signature.GenericTypeParameters}(");
            writer.Write(signature.Parameters);
            writer.WriteLine($"){signature.GenericConstraints}");

            // 3. Emit the method body (PBI 2.4: Passthrough implementation)
            writer.OpenBlock();
            EmitPassthroughBody(writer, method, signature);
            writer.CloseBlock();
        }

        private static void EmitInterceptsLocationAttribute(IndentedWriter writer, InterceptionLocation location)
        {
            // Use C# Raw String Literal ("""...""") for the file path.
            // This handles backslashes and quotes correctly without manual escaping.
            // PolySharp ensures this works even when targeting .NET Standard 2.0.
            writer.WriteLine($"[InterceptsLocation(\"\"\"{location.FilePath}\"\"\", {location.Line}, {location.Character})]");
        }

        private static void EmitPassthroughBody(IndentedWriter writer, IMethodSymbol method, MethodSignature signature)
        {
            // Determine the target of the call.
            string callTarget;
            if (signature.IsInstanceMethod)
            {
                // Use the instance parameter name.
                callTarget = MethodSignature.InstanceParameterName;
            }
            else
            {
                // Use the fully qualified type name for static methods.
                callTarget = method.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Included));
            }

            // Construct the call expression.
            // Note: We reuse GenericTypeParameters here for the call arguments.
            string callExpression = $"{callTarget}.{method.Name}{signature.GenericTypeParameters}({signature.Arguments});";

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