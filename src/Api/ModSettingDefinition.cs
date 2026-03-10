using System;

namespace ModManagerSettings.Api;

/// <summary>
/// Shared value callbacks/defaults for any typed setting definition.
/// </summary>
/// <typeparam name="T">The setting value type (bool, double, string, etc.).</typeparam>
public abstract class ModSettingDefinition<T> : ModSettingDefinitionBase
{
    public T DefaultValue { get; init; } = default!;

    public Func<T>? GetCurrentValue { get; init; }

    /// <summary>
    /// Legacy callback for immediate UI changes.
    /// ModManagerSettings now treats Apply as authoritative and does not invoke this callback.
    /// Keep this only for backwards compatibility with older registrations.
    /// </summary>
    public Action<T>? OnChanged { get; init; }

    /// <summary>
    /// Optional callback invoked when the user presses the submenu Apply button.
    /// Use this when a setting should only commit after explicit confirmation.
    /// </summary>
    public Action<T>? OnApply { get; init; }
}
