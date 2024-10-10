using VerifyRefactoringCS = KindaUselessAnalyzers.Tests.RoslynUtils.CSharpCodeRefactoringVerifier<KindaUselessAnalyzers.StringToCharArrayRefactoringProvider>;

namespace KindaUselessAnalyzers.Tests;

public class StringToCharArrayRefactoringProviderTests {
    [Fact]
    public async Task RefactoringShouldRefactor() {
        var test = new VerifyRefactoringCS.Test {
            TestState =
            {
                Sources = { """
public class Program {
    public static void Main() {
        var myString = [|"Hello, World!"|];
    }
}
""" }
            },
            FixedCode = """
public class Program {
    public static void Main() {
        var myString = string.Join('H', 'e', 'l', 'l', 'o', ',', ' ', 'W', 'o', 'r', 'l', 'd', '!');
    }
}
"""
        };

        await test.RunAsync();
    }
}
