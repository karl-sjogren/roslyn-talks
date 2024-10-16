namespace SeriousSourceGenerator.TestApp;

public partial class StringContainer {
    [GenerateStringVariants]
    public string NormalString { get; } = "Hello, World!";

    [GenerateStringVariants]
    public string PascalString { get; } = "HelloWorld!";
}
