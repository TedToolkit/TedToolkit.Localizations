---
name: generate-typed-localizations
description: >-
  Add or change compile-time JSON translations with TedToolkit.Localizations when a .NET project uses generated localization properties or methods.
---

# Generate Typed Localizations

Start with the base JSON because its file name and key shape determine the generated C# types and members. Keep translations aligned with the base keys.

## Workflow

- Locate the project’s configured localization files. Add the key to the required base `Localization.json`, then add matching keys to the culture-specific files.
- Keep `{{placeholder}}` names consistent between the base and translated strings. Build to confirm the generated member has the expected parameters and namespace.
- Call the generated member rather than looking up a string key manually. Check the target culture, fallback to the base resource, and interpolation with representative values.

## Example

```json
{
  "Hello": "Nice to meet '{{You}}'!",
  "Sub": {
    "Nice": "Great!\nThanks!"
  }
}
```

```xml
<ItemGroup>
  <AdditionalFiles Include="Localizations\Localization*.json" />
</ItemGroup>
```

## Check the result

When a member is missing, verify that the JSON is included as an `AdditionalFiles` item and that the generator sees the base resource. Confirm the generated C# method signature, fallback culture, and placeholder values in a consumer.
