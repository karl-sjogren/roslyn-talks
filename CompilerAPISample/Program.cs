using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

var code = @"
using System;

public class Program
{
    public void Main()
    {
        Console.WriteLine(""Hello, World!"");
    }
}
";

// Hämta referenser till ett par .NET-bibliotek
var coreLib = typeof(object).Assembly.Location;

var referencePaths = new[] { "System.Runtime.dll", "System.Console.dll" }
    .Select(file => Path.Combine(Path.GetDirectoryName(coreLib)!, file))
    .Prepend(coreLib);

// Skapa en syntaxträd från källkoden och kompilera till en assambly
var syntaxTree = CSharpSyntaxTree.ParseText(code);
var compilation = CSharpCompilation.Create("MyAssembly",
        options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
        references: referencePaths.Select(path => MetadataReference.CreateFromFile(path))
    )
    .AddSyntaxTrees(syntaxTree);

var compilationResult = compilation.Emit("MyAssembly.dll");

if (compilationResult.Success) {
    Console.WriteLine("Compilation successful!");
} else {
    Console.WriteLine("Compilation failed!");
    foreach (var diagnostic in compilationResult.Diagnostics) {
        Console.WriteLine(diagnostic);
    }
    return;
}

// Ladda in den kompilerade assamblyn och anropa Program.Main
var program = Activator.CreateInstanceFrom("MyAssembly.dll", "Program")!.Unwrap();
var main = program!.GetType().GetMethod("Main");
main!.Invoke(program, []);
