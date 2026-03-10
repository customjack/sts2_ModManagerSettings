using System;
using ModManagerSettings.Api;
using MegaCrit.Sts2.Core.Logging;

namespace ModManagerSettings.Features.Examples;

internal static class BuiltInExampleSettingsRegistration
{
    private static bool _autoSyncEnabled = true;
    private static bool _showDebugOverlay;
    private static double _difficultyMultiplier = 1.25d;
    private static string _profilePreset = "Balanced";
    private static string _playerAlias = "SpireTester";
    private static string _accentColor = "#50A8FFFF";

    /// <summary>
    /// Registers tutorial/example settings rows for this mod so other modders can copy the pattern.
    /// </summary>
    public static void Register()
    {
        ModSettingsRegistry.Register(new ModSettingsRegistration
        {
            ModPckName = "ModManagerSettings",
            DisplayName = "ModManagerSettings (Examples)",
            Description = "Tutorial rows showing how to register toggle, numeric, choice, and text inputs.",
            ToggleSettings =
            [
                new ModSettingToggleDefinition
                {
                    Key = "auto_sync",
                    Label = "Auto Sync",
                    Description = "Example toggle committed only when Apply is pressed.",
                    DefaultValue = true,
                    GetCurrentValue = () => _autoSyncEnabled,
                    OnApply = value =>
                    {
                        _autoSyncEnabled = value;
                        Log.Info($"[ModManagerSettings] Example toggle applied: auto_sync={value}.");
                    }
                },
                new ModSettingToggleDefinition
                {
                    Key = "show_debug_overlay",
                    Label = "Show Debug Overlay",
                    Description = "Second example toggle for boolean setting registration.",
                    DefaultValue = false,
                    GetCurrentValue = () => _showDebugOverlay,
                    OnApply = value =>
                    {
                        _showDebugOverlay = value;
                        Log.Info($"[ModManagerSettings] Example toggle applied: show_debug_overlay={value}.");
                    }
                }
            ],
            NumberSettings =
            [
                new ModSettingNumberDefinition
                {
                    Key = "difficulty_multiplier",
                    Label = "Difficulty Multiplier",
                    Description = "Example number input (SpinBox) with min/max/step.",
                    DefaultValue = 1.25d,
                    MinValue = 0.5d,
                    MaxValue = 3.0d,
                    Step = 0.05d,
                    GetCurrentValue = () => _difficultyMultiplier,
                    OnApply = value =>
                    {
                        _difficultyMultiplier = value;
                        Log.Info($"[ModManagerSettings] Example number applied: difficulty_multiplier={value:F2}.");
                    }
                }
            ],
            ChoiceSettings =
            [
                new ModSettingChoiceDefinition
                {
                    Key = "profile_preset",
                    Label = "Profile Preset",
                    Description = "Example dropdown/choice input.",
                    Options = ["Casual", "Balanced", "Hardcore"],
                    DefaultValue = "Balanced",
                    GetCurrentValue = () => _profilePreset,
                    OnApply = value =>
                    {
                        _profilePreset = value;
                        Log.Info($"[ModManagerSettings] Example choice applied: profile_preset='{value}'.");
                    }
                }
            ],
            TextSettings =
            [
                new ModSettingTextDefinition
                {
                    Key = "player_alias",
                    Label = "Player Alias",
                    Description = "Example text input (LineEdit).",
                    PlaceholderText = "Type a nickname",
                    DefaultValue = "SpireTester",
                    GetCurrentValue = () => _playerAlias,
                    OnApply = value =>
                    {
                        _playerAlias = value;
                        Log.Info($"[ModManagerSettings] Example text applied: player_alias='{value}'.");
                    }
                }
            ],
            ColorSettings =
            [
                new ModSettingColorDefinition
                {
                    Key = "accent_color",
                    Label = "Accent Color",
                    Description = "Example color input. Accepts #RRGGBB, #RRGGBBAA, or r,g,b,a.",
                    PlaceholderText = "#50A8FFFF",
                    DefaultValue = "#50A8FFFF",
                    GetCurrentValue = () => _accentColor,
                    OnApply = value =>
                    {
                        _accentColor = value;
                        Log.Info($"[ModManagerSettings] Example color applied: accent_color='{value}'.");
                    }
                }
            ],
            OnApply = () =>
            {
                Log.Info(
                    "[ModManagerSettings] Example Apply invoked: " +
                    $"auto_sync={_autoSyncEnabled}, " +
                    $"show_debug_overlay={_showDebugOverlay}, " +
                    $"difficulty_multiplier={_difficultyMultiplier:F2}, " +
                    $"profile_preset='{_profilePreset}', " +
                    $"player_alias='{_playerAlias}', " +
                    $"accent_color='{_accentColor}'.");
            },
            OnRestoreDefaults = RestoreDefaults
        });
    }

    private static void RestoreDefaults()
    {
        _autoSyncEnabled = true;
        _showDebugOverlay = false;
        _difficultyMultiplier = 1.25d;
        _profilePreset = "Balanced";
        _playerAlias = "SpireTester";
        _accentColor = "#50A8FFFF";

        Log.Info("[ModManagerSettings] Example defaults restored.");
    }
}
