namespace ModManagerSettings.Features.Screens.SettingsSubmenu.Rows;

/// <summary>
/// Implemented by setting rows that can commit their current UI value during Apply.
/// </summary>
internal interface IApplySettingRow
{
    bool ApplyPending();
}

/// <summary>
/// Implemented by setting rows that can reset their local UI value to default.
/// Reset does not auto-apply; it only updates pending UI state.
/// </summary>
internal interface IResetSettingRow
{
    void ResetToDefault();
}

/// <summary>
/// Implemented by setting rows that can be serialized/deserialized for profile persistence.
/// </summary>
internal interface IPersistableSettingRow
{
    string SettingKey { get; }

    string SerializeCurrentValue();

    bool TrySetSerializedValue(string value);
}
