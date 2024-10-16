
using VerifyCS = KindaUselessAnalyzers.Tests.RoslynUtils.CSharpAnalyzerVerifier<KindaUselessAnalyzers.KindaUselessAnalyzers>;

namespace KindaUselessAnalyzers.Tests;

public class KindaUselessAnalyzersTests {
    [Fact]
    public async Task FindsString() {
        var test = new VerifyCS.Test {
            TestState =
            {
                Sources = { """
public class Program {
    public static void Main() {
        var normalString = [|"Hello, World!"|];
    }
}
""" }
            }
        };

        await test.RunAsync();
    }

    [Fact]
    public async Task FindsStringExplicit() {
        var test = new VerifyCS.Test {
            TestState =
            {
                Sources = { """
public class Program {
    public static void Main() {
        var normalString = {|#0:"Hello, World!"|};
    }
}
""" }
            },
            ExpectedDiagnostics = {
                VerifyCS
                    .Diagnostic()
                    .WithLocation(0)
                    .WithArguments("\"Hello, World!\"")
            }
        };

        await test.RunAsync();
    }

    [Fact]
    public async Task FindStringsInDifferenctLocations() {
        var test = new VerifyCS.Test {
            TestState =
            {
                Sources = { """
using System;

public class Program {
    private const string _constString = [|"Hello, World!"|];

    public static void Main() {
        Console.WriteLine([|"Hello, World!"|]);

        var normalString = [|"Hello, World!"|];
        var verbatimString = [|@"Hello, World!"|];
    }
}
""" }
            }
        };

        await test.RunAsync();
    }
}
