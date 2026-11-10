---
name: generate-typed-localizations
description: >-
  Add or change compile-time JSON translations with TedToolkit.Localizations when a .NET project uses generated localization properties or methods.
---

# Generate Typed Localizations

- Locate the project’s configured localization files. Add the key to the required base `Localization.json`, then add matching keys to the culture-specific files.
- Keep `{{placeholder}}` names consistent between the base and translated strings. Build to confirm the generated member has the expected parameters and namespace.
- Call the generated member rather than looking up a string key manually. Check the target culture, fallback to the base resource, and interpolation with representative values.
