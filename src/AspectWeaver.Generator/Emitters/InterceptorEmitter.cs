// src/AspectWeaver.Generator/Emitters/InterceptorEmitter.cs
using AspectWeaver.Generator.Analysis;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Linq;
// Import the specific Roslyn namespace for literal formatting
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
            // (Implementation remains the same, but calls the fixed EmitInterceptsLocationAttribute)
            var signature = new MethodSignature(target.TargetMethod);

            // 1. Emit [InterceptsLocation] attribute
            EmitInterceptsLocationAttribute(writer, target.Location);

            // 2. Emit the method signature
            string asyncModifier = signature.IsAsync ? "async " : "";

            writer.Write($"internal static {asyncModifier}{signature.ReturnType} {interceptorName}{signature.GenericTypeParameters}(");
            writer.Write(signature.Parameters);
            writer.WriteLine($"){signature.GenericConstraints}");

            // 3. Emit the method body
            writer.OpenBlock();
            PipelineEmitter.EmitPipeline(writer, target, signature);
            writer.CloseBlock();
        }

        private static void EmitInterceptsLocationAttribute(IndentedWriter writer, InterceptionLocation location)
        {
            // FIX: Use the standard FormatLiteral signature with positional arguments for compatibility.
            // Signature: FormatLiteral(string value, bool quote)
            // This generates a correctly escaped standard C# string literal.
            string filePathLiteral = SymbolDisplay.FormatLiteral(location.FilePath, true);

            writer.WriteLine($"[InterceptsLocation({filePathLiteral}, {location.Line}, {location.Character})]");
        }
    }
}