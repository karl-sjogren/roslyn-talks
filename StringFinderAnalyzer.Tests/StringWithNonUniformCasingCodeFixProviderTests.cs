using VerifyFixCS = StringFinderAnalyzer.Tests.RoslynUtils.CSharpCodeFixVerifier<StringFinderAnalyzer.StringWithNonUniformCasingAnalyzer, StringFinderAnalyzer.StringWithNonUniformCasingCodeFixProvider>;

namespace StringFinderAnalyzer.Tests;

public class StringWithNonUniformCasingCodeFixProviderTests {
    [Fact]
    public async Task ToUpperActionShouldMakeStringUpperCase() {
        var test = new VerifyFixCS.Test {
            TestState =
            {
                Sources = { """
public class Program {
    public static void Main() {
        var badString = [|"Hello, World!"|];
    }
}
""" }
            },
            FixedCode = """
public class Program {
    public static void Main() {
        var badString = [|"HELLO, WORLD!"|];
    }
}
""",
            CodeActionEquivalenceKey = "StringWithNonUniformCasingCodeFixProvider_ToUpper"
        };

        await test.RunAsync();
    }
    [Fact]
    public async Task ToLowerActionShouldMakeStringLowerCase() {
        var test = new VerifyFixCS.Test {
            TestState =
            {
                Sources = { """
public class Program {
    public static void Main() {
        var badString = [|"Hello, World!"|];
    }
}
""" }
            },
            FixedCode = """
public class Program {
    public static void Main() {
        var badString = [|"hello, world!"|];
    }
}
""",
            CodeActionEquivalenceKey = "StringWithNonUniformCasingCodeFixProvider_ToLower"
        };

        await test.RunAsync();
    }
}