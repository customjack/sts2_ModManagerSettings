namespace ModManagerSettings.Features.SettingsSubmenu.Rows;

/// <summary>
/// Implemented by setting rows that can commit their current UI value during Apply.
/// </summary>
internal interface IApplySettingRow
{
    bool ApplyPending();
}
