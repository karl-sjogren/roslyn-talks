
using VerifyCS = KindaUselessAnalyzers.Tests.RoslynUtils.CSharpAnalyzerVerifier<KindaUselessAnalyzers.CommentAnnotationAnalyzer>;

namespace KindaUselessAnalyzers.Tests;

public class CommentAnnotationAnalyzerTests {
    [Fact]
    public async Task FindsString() {
        var test = new VerifyCS.Test {
            TestState =
            {
                Sources = { """
public class Program {
    public static void Main() {
        [|// This isn't good enough!|]

        // 2021-10-01 KJ This is fine
        // And this is fine because above!

        [|/*
            This isn't good enough!

        */|]

        /*
            2021-10-01 KJ This is fine
        */
    }
}
""" }
            }
        };

        await test.RunAsync();
    }
}
