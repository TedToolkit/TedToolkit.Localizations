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
/// Incremental source generator that reads <c>Localization*.json</c> additional files
/// and emits a strongly-typed <c>Localization</c> class with culture-aware string lookups.
/// </summary>
[Generator(LanguageNames.CSharp)]
public class LocalizationGenerator : IIncrementalGenerator
{
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Use the consuming assembly's name as the generated namespace
        var assemblyNameProvider = context.CompilationProvider
            .Select((compilation, _) => compilation.AssemblyName ?? "TedToolkit.Localizations");

        // Collect all Localization*.json additional files
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

    /// <summary>
    /// Parses all JSON files, groups them by culture, and emits the <c>Localization</c> class.
    /// </summary>
    /// <param name="context">The source production context for reporting diagnostics and adding sources.</param>
    /// <param name="item">A tuple of collected JSON additional texts and the target namespace (assembly name).</param>
    private static void Generate(SourceProductionContext context,
        (ImmutableArray<AdditionalText> Left, string Right) item)
    {
        var (files, nameSpace) = item;

        // Parse each JSON file and key it by culture code ("" for the base file)
        var dict = new Dictionary<string, JObject>();
        foreach (var additionalText in files)
        {
            var fileNames = Path.GetFileNameWithoutExtension(additionalText.Path).Split('.');
            var culture = fileNames.Length > 1 ? fileNames[fileNames.Length - 1] : "";

            var jsonString = additionalText.GetText(context.CancellationToken)?.ToString();
            if (jsonString is null)
            {
                continue;
            }

            if (JsonConvert.DeserializeObject(jsonString) is not JObject jsonObject)
            {
                continue;
            }

            dict[culture] = jsonObject;
        }

        // The base Localization.json (culture "") is required
        if (!dict.TryGetValue("", out var mainObject))
        {
            return;
        }

        dict.Remove("");

        var classDeclaration = Class("Localization").Public.Static.Partial;

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

    /// <summary>
    /// Recursively processes a JSON key-value pair and appends the corresponding member
    /// (nested class, property, or method) to <paramref name="declaration"/>.
    /// </summary>
    /// <param name="declaration">The parent type declaration to add the member to.</param>
    /// <param name="key">The current JSON key (used as the member name).</param>
    /// <param name="totalKey">The dot-separated full path from root (e.g. <c>"Sub.Nice"</c>), used for runtime override lookups.</param>
    /// <param name="value">The JSON value from the base translation file.</param>
    /// <param name="otherKeys">Culture-specific values for this key, keyed by culture code.</param>
    private static void AppendMember(
        TypeDeclaration declaration,
        string key,
        string totalKey,
        JToken? value,
        Dictionary<string, JToken?> otherKeys)
    {
        // Nested JSON objects become nested static classes
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

            // Emits a private Get{Key}() method with a switch on Culture for locale resolution
            void AddGetMethod()
            {
                var method = Method(methodName, new(DataType.String)).Private.Static;

                var switchStatement =
                    new SwitchStatement("global::TedToolkit.Localizations.LocalizationSettings.Culture".ToSimpleName());

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
                    .AddStatement("global::TedToolkit.Localizations.LocalizationSettings.LocalizedStrings.TryGetValue"
                        .ToSimpleName().Invoke()
                        .AddArgument(Argument(totalKey.ToLiteral()))
                        .AddArgument(Argument(new VariableExpression(DataType.Var, "value")).Out).If
                        .AddStatement("@value".ToSimpleName().Return))
                    .AddStatement(stringValue.ToLiteral().Return));

                declaration.AddMember(method
                    .AddStatement(switchStatement));
            }

            // Emits the public-facing member: a property for plain strings, a method for parameterized strings
            void AddMethodOrProperty()
            {
                var summary = new DescriptionSummary(
                    new DescriptionText(
                        ZString.Concat("Gets the localized text for key: <c><b>", totalKey, "</b></c>.")),
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

    /// <summary>
    /// Matches <c>{{parameter}}</c> placeholders in translation strings.
    /// </summary>
    private static readonly Regex _argumentRegex = new(@"\{\{(.*?)\}\}");

    /// <summary>
    /// Converts <c>{{param}}</c> placeholders to XML doc markup (<c>&lt;c&gt;&lt;b&gt;param&lt;/b&gt;&lt;/c&gt;</c>).
    /// </summary>
    /// <param name="value">The raw translation string containing <c>{{placeholder}}</c> tokens.</param>
    private static string ToDescription(string value)
    {
        return value.Replace("{{", "<c><b>").Replace("}}", "</b></c>");
    }
}