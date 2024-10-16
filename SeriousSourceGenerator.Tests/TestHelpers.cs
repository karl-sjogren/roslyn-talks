using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SeriousSourceGenerator.Tests;

// Adapted from https://github.com/andrewlock/NetEscapades.EnumGenerators/blob/3a2290852530af8f826084bc6a7d1b7850abc496/tests/NetEscapades.EnumGenerators.Tests/TestHelpers.cs
// MIT License

internal static class TestHelpers {
    public static (ImmutableArray<Diagnostic> Diagnostics, SourceOutput[] Output) GetGeneratedOutput<T>(string source)
        where T : IIncrementalGenerator, new() {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(_ => !_.IsDynamic && !string.IsNullOrWhiteSpace(_.Location))
            .Select(_ => MetadataReference.CreateFromFile(_.Location))
            .Concat(new[]
            {
                MetadataReference.CreateFromFile(typeof(T).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(GenerateStringVariantsAttribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.ComponentModel.DataAnnotations.DisplayAttribute).Assembly.Location),
            });

        var compilation = CSharpCompilation.Create(
            "generator",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var originalTreeCount = compilation.SyntaxTrees.Length;
        var generator = new T();

        var driver = CSharpGeneratorDriver.Create(generator);
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        var sourceOutputs = outputCompilation
            .SyntaxTrees
            .Skip(1)
            .Select(x => new SourceOutput(Path.GetFileName(x.FilePath), x.ToString()))
            .ToArray();

        return (diagnostics, sourceOutputs);
    }
}

internal record SourceOutput(string Filename, string Output);
