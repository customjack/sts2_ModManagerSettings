namespace ModManagerSettings.Api;

/// <summary>
/// Shared metadata for all setting definition types.
/// </summary>
public abstract class ModSettingDefinitionBase
{
    public required string Key { get; init; }

    public required string Label { get; init; }

    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Optional directory-style grouping path for ModManagerSettings UI.
    /// Examples:
    /// - "Settings"
    /// - "Settings/Routing/Constraints"
    /// - "Advanced/Debug"
    /// Defaults to "Settings".
    /// </summary>
    public string Path { get; init; } = "Settings";
}
