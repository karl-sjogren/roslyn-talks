# .NET Compiler Platform (Roslyn)

## Introduktion

Kompilatorn för C# och VB.NET, är open source of finns på <https://github.com/dotnet/roslyn>.

Parsar C# och VB.NET-kod och skapar ett syntaxträd (AST) som används för att generera IL-kod.

IL-koden kompileras sen till maskin-kod av RyuJIT när koden körs.

VB.Net-delen är skriven i VB.NET medan C#-delen samt delad infrastruktur är skriven i C#.

## Kompilering av kod

`csc` och `vbc` är kompilatorerna som körs i bakgrunden när man kompilerar sitt projekt
i Visual Studio eller med `dotnet build`.

Dom ser man dock väldigt sällan, annat än möjligtvis i något felmeddelande när kompileringen
misslyckas.

Roslyn finns också tillgängligt som ett NuGet-paket som tillåter en att kompilera kod
dynamiskt i en applikation.

```csharp
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

var syntaxTree = CSharpSyntaxTree.ParseText(code);
var compilation = CSharpCompilation.Create("MyAssembly")
    .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
    .AddSyntaxTrees(syntaxTree);

compilation.Emit("MyAssembly.dll");
```

### MSBuild

MSBuild är separat från Roslyn men använder `csc` och `vbc` för att kompilera koden.
Det som MSBuild tillför till det hela är en struktur för att beskriva hur koden ska
kompileras. I sin yttersta form så är detta `.csproj` eller `.vbproj`-filer men bakom
dessa så finns det en uppsjö `.target`och `.props`-filer.

I sin enklaste form skulle en msbuild-fil kunna se ut så här.

```xml
<Project>
    <ItemGroup>
        <Compile Include="Program.cs"/>
    </ItemGroup>
    <Target Name="Build">
        <Csc Sources="@(Compile)"/>
    </Target>
</Project>
```

För .NET Framework så hade man kunnat kompilera detta, för .NET så krävs det att man
registrerar en massa grundläggange assemblies. Dessa referenser (och mycket annat)
får man vanligtvis via en `Sdk` som `Microsoft.NET.Sdk` eller `Microsoft.NET.Sdk.Web`.

```xml
<Project Sdk="Microsoft.NET.Sdk">
</Project>
```

### Verktyg för att inspektera syntaxträd/IL-kod

- SharpLab <https://sharplab.io>
- Roslyn Quoter <https://roslynquoter.azurewebsites.net>
- LINQPad <https://www.linqpad.net>

## Syntax API / AST

En AST (Abstract Syntax Tree) är en objektstruktur som representerar den parsade koden
i ett program. Detta är inget unikt i Roslyn utan man stöter på det även i t.ex. javascript
och andra programspråk. I Roslyn-världen så pratar man oftast inte om AST utan om Syntax API.

Fördelen med att parsa upp koden i ett syntaxträd är att det är relativt enkelt att både granska
och manipulera koden. Detta är något som används flitigt i analyzers och codefixes.

Syntaxträdet innehåller information om allt i koden, från namespace och klasser till whitespace
(om man inte väljer att exkludera det som kallas för "trivia"). Detta gör att det väldigt snabbt
blir väldigt mycket information.

```csharp
using System;

Console.WriteLine("Hello world");
```

Detta parsas upp till ett träd motsvarande detta.

```csharp
CompilationUnit()
    .WithUsings(
        SingletonList<UsingDirectiveSyntax>(
            UsingDirective(
                IdentifierName("System"))))
    .WithMembers(
        SingletonList<MemberDeclarationSyntax>(
            GlobalStatement(
                ExpressionStatement(
                    InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName("Console"),
                            IdentifierName("WriteLine")))
                    .WithArgumentList(
                        ArgumentList(
                            SingletonSeparatedList<ArgumentSyntax>(
                                Argument(
                                    LiteralExpression(
                                        SyntaxKind.StringLiteralExpression,
                                        Literal("Hello world"))))))))))
    .NormalizeWhitespace()
```

Vill man enkelt inspektera ett syntaxträd så kan man göra detta på t.ex. [SharpLab](https://sharplab.io/#v2:C4LghgzsA0AmIGoA+ABATARgLAChcowE4AKAIgAkBTAG2oHsACAdzoCdrZSBKAbiA===) eller med t.ex. LINQPad.

## Analyzers och CodeFixes

Analyzers används för att uppmärksamma kod som inte följer en viss standard eller best practice.
Eller som bara inte gör som man vill. Dom används också som instegspunkter för CodeFixes som är
automatiserade sätt att justera kod.

Nedan är ett exempel på en väldigt enkel analyzer som uppmärksammar alla strängar i koden.

```csharp
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class StringFinderAnalyzer : DiagnosticAnalyzer {
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [ _diagnosticDescriptor ];

    private DiagnosticDescriptor _diagnosticDescriptor { get; } = new(
            id: "XA0001",
            title: "This is a string",
            messageFormat: "Move a long, nothing to see here. Just a string: {0}",
            category: "XLENT",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

        context.RegisterSyntaxNodeAction(CheckStringLiteralTokens, SyntaxKind.StringLiteralExpression);
    }

    private void CheckStringLiteralTokens(SyntaxNodeAnalysisContext context) {
        if (context.Node is not LiteralExpressionSyntax literalNode) {
            return;
        }

        var diagnostic = Diagnostic.Create(
            descriptor: _diagnosticDescriptor,
            location: literalNode.GetLocation(),
            messageArgs: [literalNode.Token.Text]);

        context.ReportDiagnostic(diagnostic);
    }
}
```

Alla analyzers ärver från `DiagnosticAnalyzer` och implementerar `Initialize`-metoden. I
denna så registrerar man en eller flera callbacks som körs när en viss typ av syntaxnod
påträffas. I exemplet ovan så registreras en callback specifikt för en `StringLiteralExpression`
men mer avancerad analyzers kan registrera callbacks för flera olika typer av syntaxnoder.

I callbacken som registreras så kan man sen inspektera noden och skapa upp en `Diagnostic`
som beskriver vad som är fel.

Detta är allt som behövs för att skapa en enkel analyzer. Om man lägger den i ett projekt
som har netstandard2.0 som target så kan man sen antingen publicera detta som ett NuGet-paket
eller referera det direkt i sitt projekt.

Om man vill referera det som ett projekt så använder man en vanlig `ProjectReference` där man
sätter `OutputItemType="Analyzer"`.

```xml
  <ItemGroup>
    <ProjectReference
      Include="..\StringFinderAnalyzer\StringFinderAnalyzer.csproj"
      PrivateAssets="all"
      ReferenceOutputAssembly="false"
      OutputItemType="Analyzer" />
  </ItemGroup>
```

### Enhetstesta analyzers

TODO

### CodeFixes

När man har en analyzer som genererar en `Diagnostic` så kan man även skapa en `CodeFix` för
att rätta till problemet som analyzern hittat. En `CodeFix` kopplas till ett eller flera
`DiagnosticId`. En `CodeFix`kan föreslå flera olika sätt att rätta till problemet och användaren
får sen välja vilken av dessa som ska användas.

Nedan är ett exempel på en `CodeFix` som kan ändra en sträng till att vara uppercase eller
lowercase, som identifieras via en analyzer som letar strängar som blandar stora och små
bokstäver.

```csharp
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(StringWithNonUniformCasingCodeFixProvider)), Shared]
public sealed class StringWithNonUniformCasingCodeFixProvider : CodeFixProvider {
    public override ImmutableArray<string> FixableDiagnosticIds => ["XA0002"];

    public override FixAllProvider? GetFixAllProvider() => null;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context) {
        var root = await context.Document.GetSyntaxRootAsync();
        if (root == null)
            return;

        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var literalSyntax = root.FindToken(diagnosticSpan.Start);
        if (literalSyntax == default)
            return;

        string ToUpper(string text) => text.ToUpper();

        var makeStringUpperCodeAction = CodeAction.Create(
            "Make string upper case",
            cancellationToken => RefactorAsync(context.Document, literalSyntax, ToUpper, cancellationToken),
            equivalenceKey: $"{nameof(StringWithNonUniformCasingCodeFixProvider)}_ToUpper");

        context.RegisterCodeFix(makeStringUpperCodeAction, context.Diagnostics);

        string ToLower(string text) => text.ToLower();

        var makeStringLowerCodeAction = CodeAction.Create(
            "Make string lower case",
            cancellationToken => RefactorAsync(context.Document, literalSyntax, ToLower, cancellationToken),
            equivalenceKey: $"{nameof(StringWithNonUniformCasingCodeFixProvider)}_ToLower");

        context.RegisterCodeFix(makeStringLowerCodeAction, context.Diagnostics);
    }

    public static async Task<Solution> RefactorAsync(
            Document document,
            SyntaxToken literalSyntax,
            Func<string, string> convert,
            CancellationToken cancellationToken) {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null) {
            throw new InvalidOperationException("Could not get syntax root");
        }

        var newLiteral = SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.ParseToken(convert(literalSyntax.Text)));
        var newRoot = root.ReplaceNode(literalSyntax.Parent!, newLiteral);

        var newDocument = document.WithSyntaxRoot(newRoot);

        return newDocument.Project.Solution;
    }
}
```

En `CodeFix` ärver från `CodeFixProvider` och implementerar `RegisterCodeFixesAsync`. I denna
metod så får man en `CodeFixContext` som innehåller information om vilka `Diagnostic` som ska
fixas och var i koden denna finns.

Koden registrerar sen två olika `CodeAction`, en för att göra strängen uppercase och en för att
göra den lowercase. Om användaren väljer att aktivera någon av dessa från gränssnittet så anropas
`RefactorAsync` som byter ut strängen i koden.

### Enhetstesta CodeFixes

TODO

## Sourcegenerators

TODO
