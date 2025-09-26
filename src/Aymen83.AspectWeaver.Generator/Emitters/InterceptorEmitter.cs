// src/AspectWeaver.Generator/Emitters/InterceptorEmitter.cs
using Aymen83.AspectWeaver.Generator.Analysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using System.Linq;

namespace Aymen83.AspectWeaver.Generator.Emitters
{
    internal static class InterceptorEmitter
    {
        private const string GeneratedNamespace = "Aymen83.AspectWeaver.Generated";
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
            // PBI 5.3: System.Diagnostics is required for Debugger attributes.
            writer.WriteLine("using System.Diagnostics;");
            writer.WriteLine();

            writer.WriteLine($"namespace {GeneratedNamespace}");
            writer.OpenBlock();

            // PBI 5.3: Apply Debugger attributes to the class.
            EmitDebuggerAttributes(writer);
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

        // PBI 5.3: New helper method for debugger attributes.
        private static void EmitDebuggerAttributes(IndentedWriter writer)
        {
            // Instruct the debugger to step over this generated infrastructure code.
            // We use FQNs for robustness.
            writer.WriteLine("[global::System.Diagnostics.DebuggerStepThrough]");
            writer.WriteLine("[global::System.Diagnostics.DebuggerNonUserCode]");
        }


        private static void EmitInterceptorMethod(IndentedWriter writer, InterceptionTarget target, string interceptorName)
        {
            // (Implementation remains the same as PBI 5.1)
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
            PipelineEmitter.EmitPipeline(writer, target, signature);
            writer.CloseBlock();
        }

        private static void EmitInterceptsLocationAttribute(IndentedWriter writer, InterceptionLocation location)
        {
            // (Implementation remains the same)
            string filePathLiteral = SymbolDisplay.FormatLiteral(location.FilePath, true);
            writer.WriteLine($"[InterceptsLocation({filePathLiteral}, {location.Line}, {location.Character})]");
        }

        private static void EmitPerformanceAttributes(IndentedWriter writer)
        {
            // (Implementation remains the same)
            writer.WriteLine("[global::System.Runtime.CompilerServices.MethodImpl(global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]");
        }
    }
}