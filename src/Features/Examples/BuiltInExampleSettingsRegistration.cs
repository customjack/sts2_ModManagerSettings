using System;
using ModManagerSettings.Api;

namespace ModManagerSettings.Features.Examples;

internal static class BuiltInExampleSettingsRegistration
{
    private static bool _autoSyncEnabled = true;
    private static bool _showDebugOverlay;
    private static bool _showLatencyGraph;
    private static double _difficultyMultiplier = 1.25d;
    private static double _enemyHpScale = 1.00d;
    private static double _uiScale = 1.00d;
    private static string _profilePreset = "Balanced";
    private static string _playerAlias = "SpireTester";
    private static string _logVerbosity = "Info";
    private static string _accentColor = "#50A8FFFF";

    /// <summary>
    /// Registers tutorial/example settings rows for this mod so other modders can copy the pattern.
    /// </summary>
    public static void Register()
    {
        ModSettingsRegistry.Register(new ModSettingsRegistration
        {
            ModPckName = "ModManagerSettings",
            DisplayName = "ModManagerSettings (DUMMY Examples)",
            Description = "DUMMY/TUTORIAL ONLY: all paths and settings here are sample data to demonstrate node-style Path grouping.",
            ShowSettingsButtonInModdingMenu = true,
            ExplorerDescription = "DUMMY/TUTORIAL ONLY: every setting shown in this mod's explorer is example data used to demonstrate nested config paths.",
            ToggleSettings =
            [
                new ModSettingToggleDefinition
                {
                    Key = "auto_sync",
                    Label = "[Example] Auto Sync",
                    Description = "Example toggle committed only when Apply is pressed.",
                    Path = "Examples/Settings/Multiplayer/Sync",
                    DefaultValue = true,
                    GetCurrentValue = () => _autoSyncEnabled,
                    OnApply = value =>
                    {
                        _autoSyncEnabled = value;
                    }
                },
                new ModSettingToggleDefinition
                {
                    Key = "show_debug_overlay",
                    Label = "[Example] Show Debug Overlay",
                    Description = "Example debug toggle under a deeper Advanced node.",
                    Path = "Examples/Advanced/Diagnostics/Overlay",
                    DefaultValue = false,
                    GetCurrentValue = () => _showDebugOverlay,
                    OnApply = value =>
                    {
                        _showDebugOverlay = value;
                    }
                },
                new ModSettingToggleDefinition
                {
                    Key = "show_latency_graph",
                    Label = "[Example] Show Latency Graph",
                    Description = "Example toggle in a parallel diagnostics branch.",
                    Path = "Examples/Advanced/Diagnostics/Network",
                    DefaultValue = false,
                    GetCurrentValue = () => _showLatencyGraph,
                    OnApply = value =>
                    {
                        _showLatencyGraph = value;
                    }
                }
            ],
            NumberSettings =
            [
                new ModSettingNumberDefinition
                {
                    Key = "difficulty_multiplier",
                    Label = "[Example] Difficulty Multiplier",
                    Description = "Example number input (SpinBox) with min/max/step.",
                    Path = "Examples/Settings/Gameplay/Combat",
                    DefaultValue = 1.25d,
                    MinValue = 0.5d,
                    MaxValue = 3.0d,
                    Step = 0.05d,
                    GetCurrentValue = () => _difficultyMultiplier,
                    OnApply = value =>
                    {
                        _difficultyMultiplier = value;
                    }
                },
                new ModSettingNumberDefinition
                {
                    Key = "enemy_hp_scale",
                    Label = "[Example] Enemy HP Scale",
                    Description = "Second gameplay numeric setting in the same node.",
                    Path = "Examples/Settings/Gameplay/Combat",
                    DefaultValue = 1.0d,
                    MinValue = 0.5d,
                    MaxValue = 5.0d,
                    Step = 0.05d,
                    GetCurrentValue = () => _enemyHpScale,
                    OnApply = value =>
                    {
                        _enemyHpScale = value;
                    }
                },
                new ModSettingNumberDefinition
                {
                    Key = "ui_scale",
                    Label = "[Example] UI Scale",
                    Description = "UI setting in a different path branch.",
                    Path = "Examples/Settings/UI/Display",
                    DefaultValue = 1.0d,
                    MinValue = 0.75d,
                    MaxValue = 2.0d,
                    Step = 0.05d,
                    GetCurrentValue = () => _uiScale,
                    OnApply = value =>
                    {
                        _uiScale = value;
                    }
                }
            ],
            ChoiceSettings =
            [
                new ModSettingChoiceDefinition
                {
                    Key = "profile_preset",
                    Label = "[Example] Profile Preset",
                    Description = "Example dropdown/choice input.",
                    Path = "Examples/Profile/Presets",
                    Options = ["Casual", "Balanced", "Hardcore"],
                    DefaultValue = "Balanced",
                    GetCurrentValue = () => _profilePreset,
                    OnApply = value =>
                    {
                        _profilePreset = value;
                    }
                },
                new ModSettingChoiceDefinition
                {
                    Key = "log_verbosity",
                    Label = "[Example] Log Verbosity",
                    Description = "Example diagnostics choice in a different advanced path.",
                    Path = "Examples/Advanced/Diagnostics/Logging",
                    Options = ["Error", "Warn", "Info", "Debug", "Trace"],
                    DefaultValue = "Info",
                    GetCurrentValue = () => _logVerbosity,
                    OnApply = value =>
                    {
                        _logVerbosity = value;
                    }
                }
            ],
            TextSettings =
            [
                new ModSettingTextDefinition
                {
                    Key = "player_alias",
                    Label = "[Example] Player Alias",
                    Description = "Example text input (LineEdit).",
                    Path = "Examples/Profile/Identity",
                    PlaceholderText = "Type a nickname",
                    DefaultValue = "SpireTester",
                    GetCurrentValue = () => _playerAlias,
                    OnApply = value =>
                    {
                        _playerAlias = value;
                    }
                }
            ],
            ColorSettings =
            [
                new ModSettingColorDefinition
                {
                    Key = "accent_color",
                    Label = "[Example] Accent Color",
                    Description = "Example color input. Accepts #RRGGBB, #RRGGBBAA, or r,g,b,a.",
                    Path = "Examples/Settings/UI/Theme",
                    PlaceholderText = "#50A8FFFF",
                    DefaultValue = "#50A8FFFF",
                    GetCurrentValue = () => _accentColor,
                    OnApply = value =>
                    {
                        _accentColor = value;
                    }
                }
            ],
            OnApply = null,
            OnRestoreDefaults = RestoreDefaults
        });
    }

    private static void RestoreDefaults()
    {
        _autoSyncEnabled = true;
        _showDebugOverlay = false;
        _showLatencyGraph = false;
        _difficultyMultiplier = 1.25d;
        _enemyHpScale = 1.0d;
        _uiScale = 1.0d;
        _profilePreset = "Balanced";
        _playerAlias = "SpireTester";
        _logVerbosity = "Info";
        _accentColor = "#50A8FFFF";

    }
}
