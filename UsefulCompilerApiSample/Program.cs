using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

var code = @"
using System;

public class WorldHelloer : IWorldHelloer
{
    public void SayHelloToWorld()
    {
        Console.WriteLine(""Hello, World!"");
    }
}
";

// Hämta referenser till ett par .NET-bibliotek
var coreLib = typeof(object).Assembly.Location;

var referencePaths = new[] { "System.Runtime.dll", "System.Console.dll" }
    .Select(file => Path.Combine(Path.GetDirectoryName(coreLib)!, file))
    .Prepend(coreLib)
    .Append(Assembly.GetExecutingAssembly().Location);

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
var worldHelloer = Activator.CreateInstanceFrom("MyAssembly.dll", "WorldHelloer")!.Unwrap() as IWorldHelloer;
worldHelloer!.SayHelloToWorld();
