
using VerifyCS = StringFinderAnalyzer.Tests.RoslynUtils.CSharpAnalyzerVerifier<StringFinderAnalyzer.StringFinderAnalyzer>;

namespace StringFinderAnalyzer.Tests;

public class StringFinderAnalyzerTests {
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
