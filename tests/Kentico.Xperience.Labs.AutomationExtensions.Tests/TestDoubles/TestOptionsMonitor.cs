using Microsoft.Extensions.Options;

namespace Kentico.Xperience.Labs.AutomationExtensions.Tests.TestDoubles;

internal sealed class TestOptionsMonitor<TOptions>(TOptions currentValue) : IOptionsMonitor<TOptions>
{
    public TOptions CurrentValue { get; set; } = currentValue;


    public TOptions Get(string? name) => CurrentValue;


    public IDisposable? OnChange(Action<TOptions, string?> listener) => null;
}
