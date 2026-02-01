// See https://aka.ms/new-console-template for more information

using TedToolkit.Localizations.Benchmark;

Console.WriteLine("Hello, World!");

Console.WriteLine(Localization.Hello("Ted"));
Console.WriteLine(Localization.Sub.Nice);

Localization.OnCultureChanged += Console.WriteLine;
Localization.Culture = "zh-CN";

Console.WriteLine(Localization.Hello("秋水"));
Console.WriteLine(Localization.Sub.Nice);
