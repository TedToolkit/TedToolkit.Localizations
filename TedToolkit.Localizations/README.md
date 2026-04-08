# TedToolkit.Localizations

A compile-time localization framework for .NET that generates strongly-typed C# classes from JSON translation files using a Roslyn incremental source generator.

## Features

- **Strongly-typed access** — localization keys become properties and methods, verified at compile time
- **Multi-culture support** — define translations in culture-specific JSON files with automatic fallback
- **Parameter interpolation** — use `{{placeholder}}` syntax for dynamic values in localized strings
- **XML documentation** — generated members include docs showing all available translations per key
- **Runtime overrides** — dynamically override translations via `LocalizationSettings.LocalizedStrings`
- **Culture change notifications** — subscribe to `LocalizationSettings.OnCultureChanged` to react to culture switches
- **Wide framework support** — targets .NET 6–10, .NET Framework 4.7.2+, and .NET Standard 2.0/2.1

## Installation

```shell
dotnet add package TedToolkit.Localizations
```

## Quick Start

### 1. Create JSON translation files

Create a base translation file (default/fallback language):

**`Localizations/Localization.json`**

```json
{
  "Hello": "Nice to meet '{{You}}'!",
  "Sub": {
    "Nice": "Great!\nThanks!"
  }
}
```

Add culture-specific translations by appending the culture code before `.json`:

**`Localizations/Localization.zh-CN.json`**

```json
{
  "Hello": "你好啊，{{You}}!",
  "Sub": {
    "Nice": "太棒了!\n感谢！"
  }
}
```

### 2. Register the JSON files as `AdditionalFiles`

In your `.csproj`:

```xml
<ItemGroup>
  <AdditionalFiles Include="Localizations\Localization*.json" />
</ItemGroup>
```

### 3. Use the generated code

```csharp
using TedToolkit.Localizations;
using YourAssemblyName; // generated Localization class uses your assembly name as namespace

// Access localized strings — strongly typed!
Console.WriteLine(Localization.Hello("Ted"));   // "Nice to meet 'Ted'!"
Console.WriteLine(Localization.Sub.Nice);        // "Great!\nThanks!"

// Switch culture at runtime
LocalizationSettings.Culture = "zh-CN";

Console.WriteLine(Localization.Hello("秋水"));   // "你好啊，秋水!"
Console.WriteLine(Localization.Sub.Nice);        // "太棒了!\n感谢！"
```

## How It Works

The source generator reads all `Localization*.json` files at compile time and produces a static `Localization` class:

- **Simple strings** become `static string` properties with `[MethodImpl(AggressiveInlining)]`
- **Parameterized strings** (containing `{{param}}`) become `static string` methods with typed parameters
- **Nested JSON objects** become nested static classes (e.g., `Localization.Sub.Nice`)
- **Culture resolution** uses a `switch` on `LocalizationSettings.Culture` for each key, falling back to the default translation

## JSON File Format

| File Pattern | Purpose |
|---|---|
| `Localization.json` | Base/default translations (required) |
| `Localization.{culture}.json` | Culture-specific translations (e.g., `zh-CN`, `fr-FR`, `ja-JP`) |

### Supported value types

- **Plain string** — generates a read-only property
- **String with `{{parameters}}`** — generates a method with `string` parameters
- **Nested object** — generates a nested static class

## Runtime API

### `LocalizationSettings`

| Member | Description |
|---|---|
| `Culture` | Gets or sets the current culture code. Defaults to `CultureInfo.CurrentCulture.Name`. |
| `LocalizedStrings` | A `Dictionary<string, string>` for runtime translation overrides (keyed by dot-separated path, e.g., `"Sub.Nice"`). |
| `OnCultureChanged` | An `Action<string>` event raised when `Culture` is set. |

## License

LGPL-3.0. See [COPYING](https://github.com/TedToolkit/TedToolkit.Localizations/blob/development/COPYING) and [COPYING.LESSER](https://github.com/TedToolkit/TedToolkit.Localizations/blob/development/COPYING.LESSER) for details.
