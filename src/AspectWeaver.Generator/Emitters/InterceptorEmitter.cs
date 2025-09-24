// src/AspectWeaver.Generator/Emitters/InterceptorEmitter.cs
using AspectWeaver.Generator.Analysis;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;

namespace AspectWeaver.Generator.Emitters
{
    internal static class InterceptorEmitter
    {
        // ... (Constants and Emit method remain the same)
        private const string GeneratedNamespace = "AspectWeaver.Generated";
        private const string GeneratedClassName = "Interceptors";

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
            writer.WriteLine();

            writer.WriteLine($"namespace {GeneratedNamespace}");
            writer.OpenBlock();

            writer.WriteLine($"internal static class {GeneratedClassName}");
            writer.OpenBlock();

            int counter = 0;
            foreach (var target in distinctTargets)
            {
                if (counter > 0) writer.WriteLine();

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
            var signature = new MethodSignature(target.TargetMethod);

            // 1. Emit [InterceptsLocation] attribute
            EmitInterceptsLocationAttribute(writer, target.Location);

            // PBI 5.1: 2. Emit Performance Optimization Attribute
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
            PipelineEmitter.EmitPipeline(writer, target, signature);
            writer.CloseBlock();
        }

        private static void EmitInterceptsLocationAttribute(IndentedWriter writer, InterceptionLocation location)
        {
            // (Implementation remains the same)
            // Signature: FormatLiteral(string value, bool quote)
            string filePathLiteral = SymbolDisplay.FormatLiteral(location.FilePath, true);
            writer.WriteLine($"[InterceptsLocation({filePathLiteral}, {location.Line}, {location.Character})]");
        }

        // PBI 5.1: New helper method for performance attributes.
        private static void EmitPerformanceAttributes(IndentedWriter writer)
        {
            // Encourage the JIT compiler to inline the interceptor method.
            // We use the fully qualified name (FQN) for maximum robustness.
            writer.WriteLine("[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
        }
    }
}