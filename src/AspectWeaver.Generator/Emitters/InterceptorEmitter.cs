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
            // (Distinct check remains the same)
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
            // Use the enhanced MethodSignature analysis.
            var signature = new MethodSignature(target.TargetMethod);

            // 1. Emit [InterceptsLocation] attribute
            EmitInterceptsLocationAttribute(writer, target.Location);

            // 2. Emit the method signature
            // PBI 2.6: Add 'async' modifier here if signature.IsAsync.
            writer.Write($"internal static {signature.ReturnType} {interceptorName}{signature.GenericTypeParameters}(");
            writer.Write(signature.Parameters);
            writer.WriteLine($"){signature.GenericConstraints}");

            // 3. Emit the method body (Now using PipelineEmitter)
            writer.OpenBlock();
            PipelineEmitter.EmitPipeline(writer, target, signature);
            writer.CloseBlock();
        }

        private static void EmitInterceptsLocationAttribute(IndentedWriter writer, InterceptionLocation location)
        {
            // (Implementation remains the same)
            writer.WriteLine($"[InterceptsLocation(\"\"\"{location.FilePath}\"\"\", {location.Line}, {location.Character})]");
        }

        // The EmitPassthroughBody method should be removed as it's moved to PipelineEmitter.
    }
}