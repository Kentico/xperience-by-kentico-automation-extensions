using CMS.Automation;

namespace Kentico.Xperience.Labs.AutomationExtensions.Core;

/// <summary>
/// Saves and reads <see cref="StepResult"/> entries in the <see cref="StepResultData"/> process data.
/// Custom steps use <see cref="Record"/> to support the "Previous step succeeded" condition.
/// </summary>
public static class StepResultRecorder
{
    /// <summary>
    /// Saves the result under the key of <paramref name="sourceName"/> and <paramref name="resultNumber"/>
    /// (see <see cref="StepResultKeys.Create"/>). Does nothing when the key is invalid.
    /// </summary>
    /// <param name="context">Context of the running process.</param>
    /// <param name="sourceName">Name of the <see cref="StepResultSource"/> registered for the step.</param>
    /// <param name="resultNumber">Result number selected on the step (see <see cref="StepResultNumberOptionsProvider"/>).</param>
    /// <param name="result">Result to save. Keep <see cref="StepResult.Message"/> free of personal data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> when the result was saved.</returns>
    public static async Task<bool> Record(
        AutomationProcessContext context,
        string sourceName,
        string? resultNumber,
        StepResult result,
        CancellationToken cancellationToken)
    {
        string? key = StepResultKeys.Create(sourceName, resultNumber);
        if (key is null)
        {
            return false;
        }

        var data = await context.GetProcessData<StepResultData>(cancellationToken) ?? new StepResultData();
        data.Results[key] = result;

        await context.SetProcessData(data, cancellationToken);

        return true;
    }


    /// <summary>
    /// Returns the result saved under the given key, or <c>null</c> when no result exists.
    /// </summary>
    public static StepResult? Find(StepResultData? data, string? key)
    {
        if (data is null || string.IsNullOrEmpty(key))
        {
            return null;
        }

        return data.Results.GetValueOrDefault(key);
    }
}
