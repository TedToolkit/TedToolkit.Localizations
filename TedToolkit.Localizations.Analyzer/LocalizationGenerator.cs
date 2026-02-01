// -----------------------------------------------------------------------
// <copyright file="LocalizationGenerator.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Immutable;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

using Cysharp.Text;

using Microsoft.CodeAnalysis;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using TedToolkit.RoslynHelper.Generators;
using TedToolkit.RoslynHelper.Generators.Syntaxes;

using static TedToolkit.RoslynHelper.Generators.SourceComposer;
using static TedToolkit.RoslynHelper.Generators.SourceComposer<
    TedToolkit.Localizations.Analyzer.LocalizationGenerator>;

namespace TedToolkit.Localizations.Analyzer;

/// <summary>
/// Generate the localization File.
/// </summary>
[Generator(LanguageNames.CSharp)]
public class LocalizationGenerator : IIncrementalGenerator
{
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var assemblyNameProvider = context.CompilationProvider
            .Select((compilation, _) => compilation.AssemblyName ?? "TedToolkit.Localizations");

        var jsonFiles = context.AdditionalTextsProvider
            .Where(file =>
            {
                var fileName = Path.GetFileName(file.Path);
                return fileName.StartsWith("Localization", StringComparison.Ordinal)
                       && fileName.EndsWith(".json", StringComparison.Ordinal);
            })
            .Collect();

        context.RegisterSourceOutput(jsonFiles.Combine(assemblyNameProvider), Generate);
    }

    private static void Generate(SourceProductionContext context, (ImmutableArray<AdditionalText> Left, string Right) item)
    {
        var (files, nameSpace) = item;
        var dict = new Dictionary<string, JObject>();
        foreach (var additionalText in files)
        {
            var fileNames = Path.GetFileNameWithoutExtension(additionalText.Path).Split('.');
            var culture = fileNames.Length > 1 ? fileNames[fileNames.Length - 1] : "";

            var jsonString = additionalText.GetText(context.CancellationToken)?.ToString();
            if (jsonString is null)
                continue;

            if (JsonConvert.DeserializeObject(jsonString) is not JObject jsonObject)
                continue;

            dict[culture] = jsonObject;
        }

        if (!dict.TryGetValue("", out var mainObject))
            return;

        dict.Remove("");

        var classDeclaration = Class("Localization").Public.Static.Partial
            .AddMember(Property(DataType.String, "Culture").Public.Static
                .AddAccessor(Accessor(AccessorType.GET))
                .AddAccessor(Accessor(AccessorType.SET)
                    .AddStatement("field = value".ToSimpleName())
                    .AddStatement("OnCultureChanged?.Invoke(value)".ToSimpleName()))
                .AddDefault(DataType.FromType<CultureInfo>().Type.Sub("CurrentCulture").Sub("Name")))
            .AddMember(Property(DataType.FromType<Dictionary<string, string>>(), "LocalizedStrings").Public.Static
                .AddAccessor(Accessor(AccessorType.GET))
                .AddDefault(new ObjectCreationExpression()))
            .AddMember(Event(DataType.FromType<Action<string>?>(), "OnCultureChanged").Public.Static);

        foreach (var keyValuePair in mainObject)
        {
            var key = keyValuePair.Key;

            AppendMember(classDeclaration, key, key, keyValuePair.Value,
                dict.ToDictionary(i => i.Key, i => i.Value[key]));
        }

        File()
            .AddNameSpace(NameSpace(nameSpace)
                .AddMember(classDeclaration))
            .Generate(context, "Localization");
    }

    private static void AppendMember(
        TypeDeclaration declaration,
        string key,
        string totalKey,
        JToken? value,
        Dictionary<string, JToken?> otherKeys)
    {
        if (value is JObject jObject)
        {
            var subType = Class(key).Public.Static;
            foreach (var keyValuePair in jObject)
            {
                var pairKey = keyValuePair.Key;
                AppendMember(subType, pairKey, ZString.Join('.', totalKey, pairKey), keyValuePair.Value,
                    otherKeys.ToDictionary(i => i.Key, i => i.Value?[pairKey]));
            }

            declaration.AddMember(subType);
        }
        else if (value is JValue jValue && jValue.ToString(CultureInfo.InvariantCulture) is { } stringValue
                                        && !string.IsNullOrEmpty(stringValue))
        {
            var methodName = ZString.Concat("Get", key);
            var table = new DescriptionTable(new DescriptionText("Culture"), new DescriptionText("Text"));
            AddGetMethod();
            AddMethodOrProperty();

            void AddGetMethod()
            {
                var method = Method(methodName, new(DataType.String)).Private.Static;

                var switchStatement = new SwitchStatement("Culture".ToSimpleName());

                foreach (var keyValuePair in otherKeys)
                {
                    if (keyValuePair.Value?.ToString() is not { } valueString
                        || string.IsNullOrEmpty(valueString))
                    {
                        continue;
                    }

                    table.AddItem(new DescriptionText(keyValuePair.Key),
                        new DescriptionText(ToDescription(valueString)));
                    switchStatement.AddSection(new SwitchSection()
                        .AddLabel(new SwitchLabel(keyValuePair.Key.ToLiteral()))
                        .AddStatement(valueString.ToLiteral().Return));
                }

                table.AddItem(new DescriptionText("default"), new DescriptionText(ToDescription(stringValue)));
                switchStatement.AddSection(new SwitchSection()
                    .AddLabel(new SwitchLabel())
                    .AddStatement("LocalizedStrings.TryGetValue".ToSimpleName().Invoke()
                        .AddArgument(Argument(totalKey.ToLiteral()))
                        .AddArgument(Argument(new VariableExpression(DataType.Var, "value")).Out).If
                        .AddStatement("@value".ToSimpleName().Return))
                    .AddStatement(stringValue.ToLiteral().Return));

                declaration.AddMember(method
                    .AddStatement(switchStatement));
            }

            void AddMethodOrProperty()
            {
                var summary = new DescriptionSummary(
                    new DescriptionText(ZString.Concat("Gets the localized text for key: <c><b>", totalKey, "</b></c>.")),
                    table);
                var result = _argumentRegex.Matches(stringValue);
                if (result.Count is 0)
                {
                    declaration.AddMember(Property(DataType.String, key).Public.Static
                        .AddRootDescription(summary)
                        .AddAccessor(Accessor(AccessorType.GET)
                            .AddAttribute(Attribute<MethodImplAttribute>()
                                .AddArgument(Argument(MethodImplOptions.AggressiveInlining.ToExpression())))
                            .AddStatement(methodName.ToSimpleName().Invoke().Return)));
                }
                else
                {
                    var method = Method(key, new(DataType.String)).Public.Static
                        .AddRootDescription(summary)
                        .AddAttribute(Attribute<MethodImplAttribute>()
                            .AddArgument(Argument(MethodImplOptions.AggressiveInlining.ToExpression())));
                    IExpression returnExpression = methodName.ToSimpleName().Invoke();

                    foreach (Match o in result)
                    {
                        var stringToReplace = o.Value;
                        var parameter = o.Value.Substring(2, o.Value.Length - 4);

                        method.AddParameter(Parameter(DataType.String, parameter));
                        returnExpression = returnExpression.Sub("Replace").Invoke()
                            .AddArgument(Argument(stringToReplace.ToLiteral()))
                            .AddArgument(Argument(parameter.ToSimpleName()));
                    }

                    declaration.AddMember(method.AddStatement(returnExpression.Return));
                }
            }
        }
    }

    private static readonly Regex _argumentRegex = new(@"\{\{(.*?)\}\}");

    private static string ToDescription(string value)
        => value.Replace("{{", "<c><b>").Replace("}}", "</b></c>");
}