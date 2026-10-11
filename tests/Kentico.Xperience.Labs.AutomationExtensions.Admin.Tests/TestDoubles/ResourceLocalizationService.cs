using System.Globalization;
using System.Resources;

using CMS.Core;

using Kentico.Xperience.Labs.AutomationExtensions.Resources;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.TestDoubles;

/// <summary>
/// Resolves keys from the library's English resources; unknown keys are returned as they are (like Xperience).
/// </summary>
internal sealed class ResourceLocalizationService : ILocalizationService
{
    private static readonly ResourceManager resources = new(typeof(AutomationExtensionsResources));


    public string GetString(string resourceKey, string? culture = null, bool useDefaultCulture = true) =>
        resources.GetString(resourceKey, CultureInfo.InvariantCulture) ?? resourceKey;


    public string LocalizeString(string inputText, string? culture = null, bool encode = false, bool useDefaultCulture = true) => inputText;


    public string LocalizeExpression(string expression, string? culture = null, bool encode = false,
        Func<string, string, bool, string>? getStringMethod = null, bool useDefaultCulture = true) => expression;
}
