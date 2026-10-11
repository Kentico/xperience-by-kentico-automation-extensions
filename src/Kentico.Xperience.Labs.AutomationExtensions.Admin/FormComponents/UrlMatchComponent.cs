using System.Globalization;

using CMS.Core;

using Kentico.Xperience.Admin.Base;
using Kentico.Xperience.Admin.Base.FormAnnotations;
using Kentico.Xperience.Admin.Base.Forms;
using Kentico.Xperience.Labs.AutomationExtensions.FormComponents;
using Kentico.Xperience.Labs.AutomationExtensions.UrlMatching;

[assembly: RegisterFormComponent(
    UrlMatchComponent.IDENTIFIER,
    typeof(UrlMatchComponent),
    "URL match rules")]

namespace Kentico.Xperience.Labs.AutomationExtensions.FormComponents;

/// <summary>
/// Assigns <see cref="UrlMatchComponent"/> to a string property holding <see cref="UrlMatchRuleSet"/> JSON.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class UrlMatchComponentAttribute : FormComponentAttribute
{
}


/// <summary>
/// Editor of URL rules (part, parameter, operator, value; up to <see cref="UrlMatchRuleSet.MAX_RULES"/>, all must match)
/// with a "Test a URL" preview. Stores the rules as <see cref="UrlMatchRuleSet"/> JSON; empty = no rules.
/// </summary>
/// <remarks>
/// The preview runs the server-side matcher through the <see cref="TestUrl"/> command, so marketers see exactly what the
/// step compares – there's no client-side copy of the matching logic. Texts are resolved on the server and sent in
/// <see cref="UrlMatchClientProperties.Texts"/>.
/// </remarks>
[ComponentAttribute(typeof(UrlMatchComponentAttribute))]
public sealed class UrlMatchComponent : FormComponent<FormComponentProperties, UrlMatchClientProperties, string>
{
    public const string IDENTIFIER = "Kentico.Xperience.Labs.AutomationExtensions.UrlMatch";

    /// <summary>
    /// Name of the preview command.
    /// </summary>
    public const string TEST_URL_COMMAND = "TestUrl";

    internal const string KEY_PREFIX = "labs.automationextensions.urlmatch.";

    /// <summary>
    /// Client text names and their resource keys (without <see cref="KEY_PREFIX"/>).
    /// </summary>
    internal static readonly IReadOnlyDictionary<string, string> TextKeys = new Dictionary<string, string>
    {
        ["rule"] = "rule",
        ["part"] = "part.label",
        ["parameter"] = "parameter.label",
        ["operator"] = "operator.label",
        ["value"] = "value.label",
        ["valuePattern"] = "value.pattern",
        ["addRule"] = "addrule",
        ["removeRule"] = "removerule",
        ["noRules"] = "norules",
        ["testLabel"] = "test.label",
        ["testPlaceholder"] = "test.placeholder",
        ["testButton"] = "test.button",
        ["testMatch"] = "test.match",
        ["testNoMatch"] = "test.nomatch",
        ["testNoRules"] = "test.norules",
        ["testRuleMatch"] = "test.rulematch",
        ["testRuleNoMatch"] = "test.rulenomatch",
        ["testUnrecognized"] = "test.unrecognized",
        ["testTimedOut"] = "test.timedout",
        ["testFailed"] = "test.failed",
        ["testHost"] = "test.host",
        ["testPath"] = "test.path",
        ["testQuery"] = "test.query",
        ["testNone"] = "test.none"
    };


    private readonly ILocalizationService localizationService;


    // Client resolves "UrlMatch" + "FormComponent" suffix => UrlMatchFormComponent export
    public override string ClientComponentName => "@kentico/xperience-admin-labs-automation-extensions/UrlMatch";


    public UrlMatchComponent(ILocalizationService localizationService)
        : base(localizationService)
    {
        this.localizationService = localizationService;
        AddValidationRule(new UrlMatchRulesValidationRule(localizationService));
    }


    /// <summary>
    /// Matches <see cref="UrlMatchTestArguments.Url"/> against the rules currently in the editor (not necessarily saved)
    /// and returns the result per rule and the normalized URL parts.
    /// </summary>
    [FormComponentCommand(CommandName = TEST_URL_COMMAND)]
    public Task<ICommandResponse<UrlMatchTestResult>> TestUrl(UrlMatchTestArguments args) =>
        Task.FromResult(ResponseFrom(Test(args.Rules, args.Url, localizationService)));


    /// <summary>
    /// Builds the preview result. Invalid rules give <see cref="UrlMatchTestResult.ErrorMessage"/> and no match.
    /// </summary>
    internal static UrlMatchTestResult Test(string? rules, string? url, ILocalizationService localizationService)
    {
        if (!UrlMatchRuleSet.TryParse(rules, out var ruleSet, out var error))
        {
            return new UrlMatchTestResult { ErrorMessage = GetErrorMessage(error!, localizationService) };
        }

        var normalized = NormalizedUrl.Parse(url);
        var evaluation = UrlMatcher.Create(ruleSet).Evaluate(normalized);

        return new UrlMatchTestResult
        {
            IsValid = true,
            IsMatch = evaluation.IsMatch,
            RuleResults = evaluation.RuleResults,
            TimedOut = evaluation.TimedOut,
            IsRecognized = normalized.IsRecognized,
            Url = normalized.Url,
            Host = normalized.Host,
            Path = normalized.Path,
            QueryParameters = normalized.QueryParameters
                .Select(parameter => new UrlMatchQueryParameter { Name = parameter.Key, Value = parameter.Value })
                .ToList()
        };
    }


    /// <summary>
    /// Returns the localized message of a rule validation error.
    /// </summary>
    internal static string GetErrorMessage(UrlMatchRuleSetError error, ILocalizationService localizationService)
    {
        string message = localizationService.GetString($"{KEY_PREFIX}error.{error.Code.ToString().ToLowerInvariant()}");
        string formatted = string.Format(
            CultureInfo.CurrentCulture,
            message,
            UrlMatchRuleSet.MAX_RULES,
            UrlMatchRuleSet.MAX_VALUE_LENGTH,
            UrlMatchRuleSet.MAX_PARAMETER_LENGTH);

        return error.RuleNumber is { } number
            ? string.Format(CultureInfo.CurrentCulture, localizationService.GetString($"{KEY_PREFIX}error.rule"), number, formatted)
            : formatted;
    }


    /// <inheritdoc />
    protected override Task ConfigureClientProperties(UrlMatchClientProperties clientProperties)
    {
        clientProperties.MaxRules = UrlMatchRuleSet.MAX_RULES;
        clientProperties.MaxValueLength = UrlMatchRuleSet.MAX_VALUE_LENGTH;
        clientProperties.MaxParameterLength = UrlMatchRuleSet.MAX_PARAMETER_LENGTH;
        clientProperties.Parts = Enum.GetValues<UrlPart>()
            .Select(part => new UrlMatchPartOption
            {
                Value = part.ToString(),
                Text = localizationService.GetString($"{KEY_PREFIX}part.{part.ToString().ToLowerInvariant()}")
            })
            .ToList();
        clientProperties.Operators = Enum.GetValues<UrlMatchOperator>()
            .Select(matchOperator => new UrlMatchOperatorOption
            {
                Value = matchOperator.ToString(),
                Text = localizationService.GetString($"{KEY_PREFIX}operator.{matchOperator.ToString().ToLowerInvariant()}"),
                RequiresValue = UrlMatchRuleSet.RequiresValue(matchOperator),
                Parts = Enum.GetValues<UrlPart>()
                    .Where(part => UrlMatchRuleSet.IsOperatorAllowed(part, matchOperator))
                    .Select(part => part.ToString())
                    .ToList()
            })
            .ToList();
        clientProperties.Texts = TextKeys.ToDictionary(item => item.Key, item => localizationService.GetString(KEY_PREFIX + item.Value));

        return Task.CompletedTask;
    }
}


/// <summary>
/// Client properties of <see cref="UrlMatchComponent"/>: limits, options, and localized texts.
/// </summary>
public sealed class UrlMatchClientProperties : FormComponentClientProperties<string>
{
    public int MaxRules { get; set; }

    public int MaxValueLength { get; set; }

    public int MaxParameterLength { get; set; }

    public IEnumerable<UrlMatchPartOption> Parts { get; set; } = [];

    public IEnumerable<UrlMatchOperatorOption> Operators { get; set; } = [];

    public IDictionary<string, string> Texts { get; set; } = new Dictionary<string, string>();
}


/// <summary>
/// URL part option of <see cref="UrlMatchComponent"/>.
/// </summary>
public sealed class UrlMatchPartOption
{
    public string Value { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;
}


/// <summary>
/// Operator option of <see cref="UrlMatchComponent"/>.
/// </summary>
public sealed class UrlMatchOperatorOption
{
    public string Value { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;

    /// <summary>Whether the value input is shown.</summary>
    public bool RequiresValue { get; set; }

    /// <summary>Parts the operator is offered for (<see cref="UrlMatchRuleSet.IsOperatorAllowed"/>).</summary>
    public IEnumerable<string> Parts { get; set; } = [];
}


/// <summary>
/// Arguments of the <see cref="UrlMatchComponent.TEST_URL_COMMAND"/> command.
/// </summary>
public sealed class UrlMatchTestArguments
{
    /// <summary>Rules JSON as currently edited.</summary>
    public string? Rules { get; set; }

    /// <summary>URL to test.</summary>
    public string? Url { get; set; }
}


/// <summary>
/// Result of the <see cref="UrlMatchComponent.TEST_URL_COMMAND"/> command.
/// </summary>
public sealed class UrlMatchTestResult
{
    /// <summary>Whether the rules are valid; otherwise <see cref="ErrorMessage"/> says why.</summary>
    public bool IsValid { get; set; }

    public string? ErrorMessage { get; set; }

    /// <summary>Whether the URL matches all rules (always true without rules).</summary>
    public bool IsMatch { get; set; }

    public IReadOnlyList<bool> RuleResults { get; set; } = [];

    /// <summary>Whether a wildcard comparison took too long (counted as "no match").</summary>
    public bool TimedOut { get; set; }

    /// <summary>Whether the URL could be parsed; unrecognized URLs have no parts.</summary>
    public bool IsRecognized { get; set; }

    public string? Url { get; set; }

    public string? Host { get; set; }

    public string? Path { get; set; }

    public IReadOnlyList<UrlMatchQueryParameter> QueryParameters { get; set; } = [];
}


/// <summary>
/// Decoded query parameter shown in the preview.
/// </summary>
public sealed class UrlMatchQueryParameter
{
    public string Name { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;
}


/// <summary>
/// Server-side validation of the stored rules (<see cref="UrlMatchRuleSet.TryParse"/>), with localized messages.
/// </summary>
internal sealed class UrlMatchRulesValidationRule(ILocalizationService localizationService) : ValidationRule<string>
{
    private readonly ILocalizationService localizationService = localizationService;


    /// <inheritdoc />
    public override Task<ValidationResult> Validate(string value, IFormFieldValueProvider formFieldValueProvider) =>
        UrlMatchRuleSet.TryParse(value, out _, out var error)
            ? ValidationResult.SuccessResult()
            : ValidationResult.FailResult(UrlMatchComponent.GetErrorMessage(error!, localizationService));
}
