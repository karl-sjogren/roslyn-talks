using SeriousSourceGenerator.TestApp;

var container = new StringContainer();
Console.WriteLine(container.NormalString);
Console.WriteLine(container.NormalStringUpperCase);
Console.WriteLine(container.NormalStringLowerCase);

Console.WriteLine(container.PascalString);
Console.WriteLine(container.PascalStringUpperCase);
Console.WriteLine(container.PascalStringLowerCase);
Console.WriteLine(container.PascalStringTitleCase);
