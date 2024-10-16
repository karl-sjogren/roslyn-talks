namespace SeriousSourceGenerator;

internal readonly record struct SourcePropertyInfo {
    public readonly string ClassName { get; }
    public readonly string ClassModifier { get; }
    public readonly string FullyQualifiedClassName { get; }
    public readonly string PropertyName { get; }
    public readonly string Namespace { get; }
    public readonly string PropertyInitializerValue { get; }

    internal SourcePropertyInfo(string className, string fullyQualifiedClassName, string classModifier, string propertyName, string @namespace, string propertyInitializerValue) {
        ClassName = className;
        ClassModifier = classModifier;
        FullyQualifiedClassName = fullyQualifiedClassName;
        PropertyName = propertyName;
        Namespace = @namespace;
        PropertyInitializerValue = propertyInitializerValue;
    }
}
