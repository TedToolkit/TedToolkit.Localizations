// -----------------------------------------------------------------------
// <copyright file="LocalizationSettings.cs" company="TedToolkit">
// Copyright (c) TedToolkit. All rights reserved.
// Licensed under the LGPL-3.0 license. See COPYING, COPYING.LESSER file in the project root for full license information.
// </copyright>
// -----------------------------------------------------------------------

using System.Globalization;

namespace TedToolkit.Localizations;

/// <summary>
/// The localization settings.
/// </summary>
public static class LocalizationSettings
{
    /// <summary>
    /// Gets or sets the culture.
    /// </summary>
    public static string Culture
    {
        get;
        set
        {
            field = value;
            OnCultureChanged?.Invoke(value);
        }
#pragma warning disable SA1500, SA1513
    } = CultureInfo.CurrentCulture.Name;
#pragma warning restore SA1513, SA1500

    /// <summary>
    /// Gets the localization strings.
    /// </summary>
    public static Dictionary<string, string> LocalizedStrings { get; } = [];

    /// <summary>
    /// On the culture changed.
    /// </summary>
#pragma warning disable CA1003
    public static event Action<string>? OnCultureChanged;
#pragma warning restore CA1003
}