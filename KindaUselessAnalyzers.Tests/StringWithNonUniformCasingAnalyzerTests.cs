
using VerifyCS = KindaUselessAnalyzers.Tests.RoslynUtils.CSharpAnalyzerVerifier<KindaUselessAnalyzers.StringWithNonUniformCasingAnalyzer>;

namespace KindaUselessAnalyzers.Tests;

public class StringWithNonUniformCasingAnalyzerTests {
    [Fact]
    public async Task FindsString() {
        var test = new VerifyCS.Test {
            TestState =
            {
                Sources = { """
public class Program {
    public static void Main() {
        var uppercaseString = "HELLO, WORLD!";
        var lowercaseString = "hello, world!";
        var badString = [|"Hello, World!"|];
    }
}
""" }
            }
        };

        await test.RunAsync();
    }
}
