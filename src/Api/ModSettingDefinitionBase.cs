namespace ModManagerSettings.Api;

/// <summary>
/// Shared metadata for all setting definition types.
/// </summary>
public abstract class ModSettingDefinitionBase
{
    public required string Key { get; init; }

    public required string Label { get; init; }

    public string Description { get; init; } = string.Empty;
}
