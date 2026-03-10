namespace ModManagerSettings.Api;

/// <summary>
/// Defines one text-input setting row contributed by another mod.
/// </summary>
public sealed class ModSettingTextDefinition : ModSettingDefinition<string>
{
    public string PlaceholderText { get; init; } = string.Empty;
}
