using System;
using System.Collections.Generic;

namespace ModManagerSettings.Api;

/// <summary>
/// Describes settings metadata/hooks for a target mod pck.
/// </summary>
public sealed class ModSettingsRegistration
{
    public required string ModPckName { get; init; }

    public required string DisplayName { get; init; }

    public string Description { get; init; } = string.Empty;

    public IReadOnlyList<ModSettingToggleDefinition> ToggleSettings { get; init; } = Array.Empty<ModSettingToggleDefinition>();

    public IReadOnlyList<ModSettingNumberDefinition> NumberSettings { get; init; } = Array.Empty<ModSettingNumberDefinition>();

    public IReadOnlyList<ModSettingChoiceDefinition> ChoiceSettings { get; init; } = Array.Empty<ModSettingChoiceDefinition>();

    public IReadOnlyList<ModSettingTextDefinition> TextSettings { get; init; } = Array.Empty<ModSettingTextDefinition>();

    public IReadOnlyList<ModSettingColorDefinition> ColorSettings { get; init; } = Array.Empty<ModSettingColorDefinition>();

    public Action? OnApply { get; init; }

    public Action? OnRestoreDefaults { get; init; }
}
