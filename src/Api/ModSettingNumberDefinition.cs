namespace ModManagerSettings.Api;

/// <summary>
/// Defines one numeric setting row contributed by another mod.
/// </summary>
public sealed class ModSettingNumberDefinition : ModSettingDefinition<double>
{
    public double MinValue { get; init; } = 0d;

    public double MaxValue { get; init; } = 100d;

    public double Step { get; init; } = 1d;
}
