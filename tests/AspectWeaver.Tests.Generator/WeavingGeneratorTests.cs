using VerifyXunit;
using Xunit;
using System.Threading.Tasks;

namespace AspectWeaver.Tests.Generator;

public class WeavingGeneratorTests
{
    [Fact]
    public Task Generator_WhenInputIsEmpty_ShouldOnlyGeneratePrerequisites()
    {
        // Arrange
        var input = """
                    // Empty input code
                    """;

        // Act & Assert
        // This will fail the first time until the snapshot is accepted.
        return GeneratorTestHelper.Verify(input);
    }
}