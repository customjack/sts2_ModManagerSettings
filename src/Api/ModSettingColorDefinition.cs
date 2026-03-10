namespace ModManagerSettings.Api;

/// <summary>
/// Defines one color setting row contributed by another mod.
/// Value format examples:
/// - Hex: #RRGGBB or #RRGGBBAA
/// - Comma: r,g,b,a (0-1 floats or 0-255 ints)
/// </summary>
public sealed class ModSettingColorDefinition : ModSettingDefinition<string>
{
    public string PlaceholderText { get; init; } = "#RRGGBBAA";
}
