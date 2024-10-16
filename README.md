# .NET Compiler Platform (Roslyn)

## Introduktion

Kompilatorn för C# och VB.NET heter sen rätt lång tid tillbaka Roslyn. Den ärär open source
och finns på <https://github.com/dotnet/roslyn>.

Parsar C# och VB.NET-kod och skapar ett syntaxträd (AST, Abstract Syntax Tree) som används
för att generera IL-kod. IL-koden i sig är maskinoberoende och körs i en
"virtuell maskin"/runtime som exekverar koden eller kompilarar ner den till maskinkod.
Denna är vad som kallas "Common Language Runtime", förkortat CLR i .NET Framework och
"CoreCLR" i .NET Core/.NET 5+.

Processen som kompilerar maskinkoden heter RyuJIT och är en del av CLR/CoreCLR.

VB.Net-delen är skriven i VB.NET medan C#-delen samt delad infrastruktur är skriven i C#.

### MSBuild

`csc` och `vbc` är kompilatorerna som körs i bakgrunden när man kompilerar sitt projekt
i Visual Studio eller med `dotnet build`. Dessa ser man dock sällan då de inte anropas
direkt utan via MSBuild.

MSBuild är en väldigt generell task runner som används för att bygga alla möjliga
typer av projekt. Om man anropar msbuild i en mapp så letar den efter filer som har
en filändelse som slutar med `proj` och kör dessa. Det är med andra ord fullt möjligt
att hitta på egna filändelser och sätta upp egna byggsystem baserat på MSBuild.

MSBuild är separat från Roslyn men använder `csc` och `vbc` för att kompilera koden.
Det som MSBuild tillför till det hela är en struktur för att beskriva hur koden ska
kompileras. I sin yttersta form så är detta `.csproj` eller `.vbproj`-filer men bakom
dessa så finns det en uppsjö `.target`och `.props`-filer.

I sin enklaste form skulle en msbuild-fil kunna se ut så här.

```xml
<Project>
    <Target Name="DoThings">
    </Target>
</Project>
```

Denna gör dock inte så mycket. Går man ett steg längre och faktiskt försöker kompilera
något så kan det se ut så här.

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
registrerar en massa grundläggande assemblies. Dessa referenser (och mycket annat)
får man vanligtvis via en `Sdk` som `Microsoft.NET.Sdk` eller `Microsoft.NET.Sdk.Web`.

```xml
<Project Sdk="Microsoft.NET.Sdk">
</Project>
```

Exempel: `dotnet msbuild -v:diag -tl:off`

## Kompilering av kod

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
SyntaxFactory.CompilationUnit()
    .WithUsings(
        SyntaxFactory.SingletonList<UsingDirectiveSyntax>(
            SyntaxFactory.UsingDirective(
                SyntaxFactory.IdentifierName("System"))))
    .WithMembers(
        SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
            SyntaxFactory.GlobalStatement(
                SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName("Console"),
                            SyntaxFactory.IdentifierName("WriteLine")))
                    .WithArgumentList(
                        SyntaxFactory.ArgumentList(
                            SyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(
                                SyntaxFactory.Argument(
                                    SyntaxFactory.LiteralExpression(
                                        SyntaxKind.StringLiteralExpression,
                                        SyntaxFactory.Literal("Hello world"))))))))))
    .NormalizeWhitespace()
```

Vill man enkelt inspektera ett syntaxträd så kan man göra detta på t.ex. [SharpLab](https://sharplab.io/#v2:C4LghgzsA0AmIGoA+ABATARgLAChcowE4AKAIgAkBTAG2oHsACAdzoCdrZSBKAbiA===) eller med LINQPad.

Ytterligare exempel.

```csharp
    return await _assetManagementContext
        .LocationSharedNetworkCostAllocations
        .AsNoTracking()
        .Where(x => networkIds.Contains(x.NetworkId))
        .Select(x => new LocationRelatedCostAllocation {
            Allocation = x.Allocation,
            Network = x.Network,
            Project = x.PremisesProject
        })
        .ToArrayAsync(cancellationToken);
````

## Analyzers, CodeFixes och Refactorings

Analyzers används för att uppmärksamma kod som inte följer en viss standard eller best practice.
Eller som bara inte gör som man vill. Dom används också som instegspunkter för CodeFixes som
används för att rätta till något en analyzer hittat. Utöver detta två så finns det också
Refactorings, som är en form av CodeFix som inte rättar till något utan istället omstrukturerar
koden.

Nedan är ett exempel på en väldigt enkel analyzer som uppmärksammar alla strängar i koden.

```csharp
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class KindaUselessAnalyzers : DiagnosticAnalyzer {
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
som har `netstandard2.0 som` target så kan man sen antingen publicera detta som ett NuGet-paket
eller referera det direkt i sitt projekt. Att referera projektet direkt kommer dock med
vissa problem och är egentligen inte rekommenderat.

Om man vill referera det som ett projekt så använder man en vanlig `ProjectReference` där man
sätter `OutputItemType="Analyzer"`.

```xml
  <ItemGroup>
    <ProjectReference
      Include="..\KindaUselessAnalyzers\KindaUselessAnalyzers.csproj"
      PrivateAssets="all"
      ReferenceOutputAssembly="false"
      OutputItemType="Analyzer" />
  </ItemGroup>
```

Vill man referera ett NuGet-paket med analyzers så gör det med en `PackageReference`.

```xml
  <ItemGroup>
    <PackageReference Include="KindaUselessAnalyzers" Version="1.0.0">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>
  </ItemGroup>
```

Genom att sätta `PrivateAssets` till `all` så undviker man att paketet propageras
till andra projekt som refererar det inkluderande projektet projekt. `IncludeAssets`
i exemplet ovan är det som är standard för ett analyzer-paket, men i de flesta fall
hade man kunnat minska ner det till enbart `analyzers`.

### Enhetstesta analyzers

Att skriva enhetstester för analyzers är relativt enkelt tack vare bra stödbibliotek.
När man genererar ett analyzer-projekt i Visual Studio. Av någon anledning finns det
inga `dotnet new`-templates för detta ännu.

Det finns ett antal klasser i mappen `RoslynUtils` som hjälper till att skapa upp
"AnalyzerVerifier", "CodeFixVerifier" och "RefactoringVerifier". I sin testklass sen
så kan man lätt sätta upp ett alias för verifiern enligt följande.

```csharp
using VerifyCS = KindaUselessAnalyzers.Tests.RoslynUtils.CSharpAnalyzerVerifier<KindaUselessAnalyzers.KindaUselessAnalyzers>;
```

Själva testet sen blir att skapa upp en kodsträng med en lite speciell syntax som
beskriver var analyzern ska markera för att testet ska passera. Man k

```csharp
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
```

Detta är det enklaste sättet att göra ett enhetstest för en analyzer, och i många
fall räcker det. Men i vissa fall kanske man vill verifiera vad analyzern hittar
också, och då kan man använda en annan syntax för att fånga upp en mer information.

```csharp
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
```

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

Att enhetstesta en `CodeFix` fungerar på samma sätt som att enhetstesta en `Analyzer`. Man
sätter upp ett alias för en `CodeFixVerifier` istället och matar den med både analyzern och
codefixen.

```csharp
using VerifyFixCS = KindaUselessAnalyzers.Tests.RoslynUtils.CSharpCodeFixVerifier<KindaUselessAnalyzers.StringWithNonUniformCasingAnalyzer, KindaUselessAnalyzers.StringWithNonUniformCasingCodeFixProvider>;
```

Sen i sitt test så skriver man hur koden ser ut före och efter att codefixen körts. Om man
har flera codefixes i samma `CodeFixProvider` så kan man även ange den `equivalenceKey`
som ska köras.

```csharp
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
```

## Sourcegenerators

En source generator är precis vad det låter som, något som genererar källkod.Man kan
generera precis vad som helst här, men det vanligaste är att matcha något som utvecklaren
bett om att få genererat. Genereringen sker som ett steg i kompileringen av koden, rentav
innan eventuella analyzers körs. Så analyzers kan hitta fel i den genereraede koden också.
Att detta genereras så pass tidigt i flödet gör också att t.ex. Intellisense kan använda
sig av den genererade koden.

Ett vanligt scenario för en source generator är att matcha en property eller metod som har
attributet `GeneratedRegexAttribute` för att generera koden för att exekvera en regular
expression.

```csharp
public partial class SampleContainer {
    [GeneratedRegex(@"^[a-zA-Z]+[0-9]*?|[0-9]*?[a-zA-Z]+$")]
    public partial Regex GetGeneratedRegex();

    [GeneratedRegex(@"^[a-zA-Z]+[0-9]*?|[0-9]*?[a-zA-Z]+$")]
    public partial Regex GeneratedRegex { get; }
}
```

Detta skapar i bakgrunden upp en partial av klassen `SampleContainer` som innehåller en
metod och en property som returnerar en `Regex` som matchar den regular expression som
angavs i attributet.

Det går att se resultatet av detta genom att i sin projektfil lägga på följande
egenskaper.

```xml
  <PropertyGroup>
    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
    <CompilerGeneratedFilesOutputPath>GeneratedSources</CompilerGeneratedFilesOutputPath>
  </PropertyGroup>
```

När man gjort detta och sen kör `dotnet build` så skapas en mapp som heter `GeneratedSources`
som innehåller den genererade koden.

### Skapa en source generator

En source generator ärver från `IIncrementalGenerator` (äldre generators kan ärva från
`ISourceGenerator` vilket var första generationens source generators) och implementerar
en `Initialize`-metod. I denna så kan man med en hjälpmetod säga att man vill inspektera
alla syntaxnoder som är markerade med ett specifikt attribut, alternativt sätta upp detta
manuellt liknande med en analzer.

I exemplet nedan så inspekterar vi alla klasser som har attributet `GenerateStringVariantsAttribute`
som då sen körs genom ett predikat där vi validerar noden ytterligare och sen körs en
transform som parsar upp noden till det vi behöver för att generera vår kod.

```csharp
private const string _markerAttributeName = "SeriousSourceGenerator.GenerateStringVariantsAttribute";

public void Initialize(IncrementalGeneratorInitializationContext context) {
    var propertiesToGenerate = context.SyntaxProvider
        .ForAttributeWithMetadataName(
            _markerAttributeName,
            predicate: CheckIfValidProperty,
            transform: GetSourcePropertyInfo)
        .Where(static m => m is not null);

    context.RegisterSourceOutput(propertiesToGenerate, Execute);
}
```

I det här exemplet så vill vi bara matcha på syntaxnoder som matchar följande regler.

- Är en property
- Har typen `string` eller `string?`
- Inte har en setter
- Har en getter
  - Men bara en auto-property
- Initieras till en strängliteral

Då skulle predikat-metoden se ut så här.

```csharp
private static bool CheckIfValidProperty(SyntaxNode node, CancellationToken _) {
    if(node is not PropertyDeclarationSyntax propertySyntax) {
        return false;
    }

    if(propertySyntax.Type.ToString() is not "string" and not "string?") {
        return false;
    }

    if(propertySyntax.AccessorList is null) {
        return false;
    }

    if(propertySyntax.AccessorList.Accessors.Any(a => a.Kind() == SyntaxKind.SetAccessorDeclaration)) {
        return false;
    }

    if(propertySyntax.AccessorList.Accessors.Any(a => a.Body is not null)) {
        return false;
    }

    if(propertySyntax.Initializer is null) {
        return false;
    }

    var initializerValueSyntax = propertySyntax.Initializer.Value;
    if(initializerValueSyntax is not LiteralExpressionSyntax) {
        return false;
    }

    return true;
}
```

Om något är markerat med attributet men inte uppfyller kraven så kommer
ingenting att genereras. En del paket här har därför en analyzer som
verifierar användningen av attributet så att man direkt kan få en varning
om man sätter attributet på en ogiltig property.

I transform-metoden sen så parsar vi upp syntaxnoden till en record med all
information vi behöver för att generera vår kod, t.ex. namespace, klassnamn,
propertynamn, osv.

```csharp
private static SourcePropertyInfo? GetSourcePropertyInfo(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken) {
    if(context.TargetSymbol is not IPropertySymbol propertySymbol || context.TargetNode is not PropertyDeclarationSyntax propertySyntax) {
        return null;
    }

    cancellationToken.ThrowIfCancellationRequested();

    var @namespace = propertySymbol.ContainingNamespace.IsGlobalNamespace ? string.Empty : propertySymbol.ContainingNamespace.ToString();

    var className = propertySymbol.ContainingType.Name;
    var classModifier = propertySymbol.ContainingType.DeclaredAccessibility.ToString().ToLowerInvariant();
    var fullyQualifiedClassName = @namespace + "." + className;

    var propertyName = propertySymbol.Name;

    var initializerValueSyntax = propertySyntax.Initializer.Value as LiteralExpressionSyntax;
    var initialValue = initializerValueSyntax.Token.Value as string;

    return new SourcePropertyInfo(className, fullyQualifiedClassName, classModifier, propertyName, @namespace, initialValue);
}
```

Sen i `Execute`-metoden så genererar vi vår kod utifrån den information vi
sammanställt.

Den genererade koden är i många fall en partial-klass som utökar den klassen där
attributet användes, men det går att lägga till helt fristående filer och klasser
också.

I detta fall så genererar vi en partial-klass, dvs vi använder samma klassnamn,
namespace, modifier som på original-klassen. Sen lägger vi till två properties
med samma namn som på original-propertyn men med suffixen `UpperCase` och
`LowerCase`. Dessa initieras sedan med `ToUpperInvariant` respektive `ToLowerInvariant`
på det värde som propertyn hade.

Sen lägger vi till denna kod i `SourceProductionContext` med ett filnamn som är
unikt för den här propertyn så vi inte riskerar att krocka om man satt attributet
på flera ställen.

```csharp
private static void Execute(SourceProductionContext context, SourcePropertyInfo? propertyToGenerate) {
    if(!(propertyToGenerate is { } property)) {
        return;
    }

    var sb = new StringBuilder();
    var value = property.PropertyInitializerValue;

    sb.AppendLine("//------------------------------------------------------------------------------");
    sb.AppendLine("// <auto-generated>");
    sb.AppendLine("//     This code was geneted by " + nameof(StringVariantSourceGenerator) + ".");
    sb.AppendLine("// </auto-generated>");
    sb.AppendLine("//------------------------------------------------------------------------------");
    sb.AppendLine();

    sb.AppendLine("namespace " + property.Namespace + ";");
    sb.AppendLine();
    sb.AppendLine("#nullable enable");
    sb.AppendLine();
    sb.AppendLine($"{property.ClassModifier} partial class {property.ClassName} {{");
    sb.AppendLine($"    public string {property.PropertyName}UpperCase {{ get; }} = \"{value.ToUpperInvariant()}\";");
    sb.AppendLine($"    public string {property.PropertyName}LowerCase {{ get; }} = \"{value.ToLowerInvariant()}\";");
    sb.AppendLine("}");

    context.AddSource(property.ClassName + "_" + property.PropertyName + "_StringVariants.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
}
```

### Enhetstesta SourceGenerators

Att enhetstesta en source generator är lite mer komplicerat än att testa en analyzer
då det inte följer med några hjälpklasser för detta. Som tur är så finns det folk
som lösta detta själva.

Följande metod är lånad från paketet `NetEscapades.EnumGenerators`. Det den gör är att
ta källkod som en sträng, parsa upp den med Roslyn-APIet, köra source generatorn och
sen returnera den genererade koden och eventuella diagnostics.

```csharp
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
```

Med hjälp av den klassen så är det sen väldigt enkelt att skriva enhetstester
som verifierer den genererade koden.

```csharp
    [Fact]
    public void WhenInvoked_WithSingleProperty_GeneratesExpectedOutput() {
        const string input = @"using SeriousSourceGenerator;

namespace MyTestNameSpace {
    internal partial class CoolClass {
        [GenerateStringVariants]
        public string? CoolProp { get; } = ""ZebraTastic!"";

        [GenerateStringVariants]
        public string? CoolPropWithSetter { get; set; } = ""ZebraTastic!"";
    }
}";
        var (diagnostics, sourceOutputs) = TestHelpers.GetGeneratedOutput<StringVariantSourceGenerator>(input);

        diagnostics.ShouldBeEmpty();

        var generatedFile = sourceOutputs.FirstOrDefault(x => x.Filename == "CoolClass_CoolProp_StringVariants.g.cs");

        generatedFile.ShouldNotBeNull();

        generatedFile.Output.ShouldBe(@"//------------------------------------------------------------------------------
// <auto-generated>
//     This code was geneted by MultiLanguageSourceGenerator.
// </auto-generated>
//------------------------------------------------------------------------------

namespace MyTestNameSpace;

#nullable enable

internal partial class CoolClass {
    public Dictionary<string, string?>? CoolPropMultiLang { get; set; }

    public Dictionary<string, string?>? GetCoolPropMultiLang() {
        if(CoolProp is null) {
            return null;
        }

        var prop = CoolPropMultiLang ??= new Dictionary<string, string?>();

        prop[""iv""] = CoolProp;

        return prop;
    }
}
", StringCompareShould.IgnoreLineEndings);
    }
```

### Interceptors

Experimentell feature i .NET 8, endast enklare demo.

TODO
