// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using TedToolkit.Localizations;
using TedToolkit.Localizations.Benchmark;

Console.WriteLine("Hello, World!");

// Use the generated localization API — default culture
Console.WriteLine(Localization.Hello("Ted"));
Console.WriteLine(Localization.Sub.Nice);

// Subscribe to culture changes, then switch to zh-CN
LocalizationSettings.OnCultureChanged += Console.WriteLine;
LocalizationSettings.Culture = "zh-CN";

Console.WriteLine(Localization.Hello("秋水"));
Console.WriteLine(Localization.Sub.Nice);