# TedToolkit.Localizations

[![NuGet](https://img.shields.io/nuget/v/TedToolkit.Localizations)](https://www.nuget.org/packages/TedToolkit.Localizations)
[![License: LGPL-3.0](https://img.shields.io/badge/License-LGPL--3.0-blue.svg)](COPYING.LESSER)

A compile-time localization framework for .NET. Define translations in JSON, get strongly-typed C# code generated via a Roslyn incremental source generator — no runtime reflection, no magic strings.

## Overview

This solution contains:

| Project | Description |
|---|---|
| **TedToolkit.Localizations** | Core library + NuGet package (bundles the analyzer) |
| **TedToolkit.Localizations.Analyzer** | Roslyn incremental source generator that reads JSON and emits C# |
| **TedToolkit.Localizations.Benchmark** | Demo app and performance benchmarks |
| **Build** | Automated build pipeline |

## Quick Start

```shell
dotnet add package TedToolkit.Localizations
```

**1. Create translation files**

```
Localizations/
  Localization.json        # default language (required)
  Localization.zh-CN.json  # Chinese (Simplified)
  Localization.fr-FR.json  # French
  ...
```

`Localization.json`:
```json
{
  "Hello": "Nice to meet '{{You}}'!",
  "Sub": {
    "Nice": "Great!\nThanks!"
  }
}
```

`Localization.zh-CN.json`:
```json
{
  "Hello": "你好啊，{{You}}!",
  "Sub": {
    "Nice": "太棒了!\n感谢！"
  }
}
```

**2. Register as `AdditionalFiles` in your `.csproj`**

```xml
<ItemGroup>
  <AdditionalFiles Include="Localizations\Localization*.json" />
</ItemGroup>
```

**3. Use the generated API**

```csharp
using TedToolkit.Localizations;

// Strongly-typed access
Console.WriteLine(Localization.Hello("Ted"));  // "Nice to meet 'Ted'!"
Console.WriteLine(Localization.Sub.Nice);       // "Great!\nThanks!"

// Switch culture at runtime
LocalizationSettings.Culture = "zh-CN";
Console.WriteLine(Localization.Hello("秋水"));  // "你好啊，秋水!"
```

## How It Works

At compile time the source generator:

1. Reads all `Localization*.json` files registered as `AdditionalFiles`
2. Generates a static `Localization` class in your assembly's namespace
3. Plain strings become **properties**, parameterized strings (`{{param}}`) become **methods**
4. Nested JSON objects become **nested static classes**
5. Each member includes a `switch` on `LocalizationSettings.Culture` for culture-aware lookup, with fallback to the default translation

At runtime, `LocalizationSettings` provides:
- `Culture` — current culture code (defaults to `CultureInfo.CurrentCulture.Name`)
- `LocalizedStrings` — `Dictionary<string, string>` for dynamic overrides
- `OnCultureChanged` — event fired when `Culture` is set

See the [NuGet package README](TedToolkit.Localizations/README.md) for full API documentation.

## Building

```shell
dotnet build
```

## License

LGPL-3.0 — see [COPYING](COPYING) and [COPYING.LESSER](COPYING.LESSER).
