// See https://aka.ms/new-console-template for more information

using TedToolkit.Localizations;
using TedToolkit.Localizations.Benchmark;

Console.WriteLine("Hello, World!");

Console.WriteLine(Localization.Hello("Ted"));
Console.WriteLine(Localization.Sub.Nice);

LocalizationSettings.OnCultureChanged += Console.WriteLine;
LocalizationSettings.Culture = "zh-CN";

Console.WriteLine(Localization.Hello("秋水"));
Console.WriteLine(Localization.Sub.Nice);
