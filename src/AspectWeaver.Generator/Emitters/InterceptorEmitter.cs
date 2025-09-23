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
            // (Implementation of Emit remains the same...)
            var distinctTargets = targets.Distinct(InterceptionTargetComparer.Instance).ToList();

            if (distinctTargets.Count == 0)
            {
                return string.Empty;
            }

            using var writer = new IndentedWriter();
            writer.WriteFileHeader();

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

            // 2. Emit the method signature
            string asyncModifier = signature.IsAsync ? "async " : "";

            // FIX: Simplified, robust formatting. Construct the full signature on one conceptual line.

            writer.Write($"internal static {asyncModifier}{signature.ReturnType} {interceptorName}{signature.GenericTypeParameters}(");
            writer.Write(signature.Parameters);
            writer.Write(")");

            // Append constraints directly (they include a leading space if they exist).
            writer.Write(signature.GenericConstraints);

            // Ensure the line ends before opening the block.
            writer.WriteLine();

            // 3. Emit the method body
            writer.OpenBlock();
            PipelineEmitter.EmitPipeline(writer, target, signature);
            writer.CloseBlock();
        }

        private static void EmitInterceptsLocationAttribute(IndentedWriter writer, InterceptionLocation location)
        {
            // (Implementation remains the same using compatible FormatLiteral)
            // Signature: FormatLiteral(string value, bool quote)
            string filePathLiteral = SymbolDisplay.FormatLiteral(location.FilePath, true);
            writer.WriteLine($"[InterceptsLocation({filePathLiteral}, {location.Line}, {location.Character})]");
        }
    }
}