using System.Runtime.CompilerServices;

namespace AspectWeaver.Tests.Generator;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        // Enable snapshot testing for Source Generators.
        VerifySourceGenerators.Initialize();
    }
}