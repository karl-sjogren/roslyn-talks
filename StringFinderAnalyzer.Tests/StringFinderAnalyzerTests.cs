
using VerifyCS = StringFinderAnalyzer.Tests.RoslynUtils.CSharpAnalyzerVerifier<StringFinderAnalyzer.StringFinderAnalyzer>;

namespace StringFinderAnalyzer.Tests;

public class StringFinderAnalyzerTests {
    [Fact]
    public async Task FindStringsInDifferenctLocations() {
        var test = new VerifyCS.Test {
            TestState =
            {
                Sources = { """
using System;

public class Program {
    public static void Main() {
        Console.WriteLine([|"Hello, World!"|]);
    }
}
""" }
            }
        };

        await test.RunAsync();
    }
}
