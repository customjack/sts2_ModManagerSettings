# ModManagerSettings

A shared settings framework for Slay the Spire 2 mods. Adds a **Settings** button to each mod's entry in the in-game mod list, providing a structured UI for mod configuration.

**Features:**
- Per-mod settings submenu accessible from the in-game mod list
- Supports toggle, color, number, text, and choice input types
- Settings are persisted between sessions
- Multiplayer-aware: optionally allow host to override client settings

This mod is a **dependency** — install it if another mod requires it.

## Install

1. Download the latest release zip from the [Releases](../../releases) page.
2. Close Slay the Spire 2.
3. In Steam, right-click `Slay the Spire 2` -> `Properties` -> `Installed Files` -> `Browse`.
4. Create a `mods` folder in the game directory if it does not exist.
5. Extract the zip and drag the `ModManagerSettings` folder into `mods`.
6. Confirm these files are present in `mods/ModManagerSettings`:
   - `ModManagerSettings.dll`
   - `ModManagerSettings.pck`
   - `ModManagerSettings.json`
7. Launch Slay the Spire 2. If prompted to enable mods, accept and relaunch.
8. In-game, go to `Settings` -> `General` -> `Mods` and enable `ModManagerSettings`.

## Usage

Once installed, mods that depend on ModManagerSettings will show a **Settings** button in the mod list. Click it to open that mod's settings panel.

## Developer Notes

**Requirements:** .NET SDK, Godot 4 export templates, WSL or Linux shell.

**Setup:**
1. Copy `.env.example` to `.env`.
2. Set `STS2_INSTALL_DIR` to your game install path.

**Build and install:**
```bash
./scripts/bash/build_and_stage.sh
./scripts/bash/make_pck.sh
./scripts/bash/install_to_game.sh
```

**Registering settings from another mod:**

Call `ModSettingsRegistry.Register` (or `UpsertRegistration`) during your mod's initialization. Use the full example below as a reference.

```csharp
// MySettingsRegistration.cs
using ModManagerSettings.Api;

public static class MySettingsRegistration
{
    // In-memory state — your mod's live values
    private static bool _featureEnabled = true;
    private static double _cooldown = 2.0d;
    private static string _mode = "Fast";
    private static string _playerTag = "Player";
    private static string _accentColor = "#50A8FFFF";

    public static void Register()
    {
        ModSettingsRegistry.Register(new ModSettingsRegistration
        {
            // Must match the mod's .pck filename (without extension)
            ModPckName = "MyMod",
            DisplayName = "My Mod",
            Description = "Settings for My Mod.",

            // Show the gear icon on the mod list entry
            ShowSettingsButtonInModdingMenu = true,

            // --- Toggle (on/off checkbox) ---
            ToggleSettings =
            [
                new ModSettingToggleDefinition
                {
                    Key = "feature_enabled",
                    Label = "Enable Feature",
                    Description = "Master toggle for the feature.",
                    // Path groups rows into a tree. Slashes create nested nodes.
                    // "Settings" is the default. Use deeper paths like "Settings/Combat"
                    // to group related rows under a sub-node.
                    Path = "Settings",
                    DefaultValue = true,
                    GetCurrentValue = () => _featureEnabled,
                    OnApply = value => _featureEnabled = value
                }
            ],

            // --- Number (SpinBox with min/max/step) ---
            NumberSettings =
            [
                new ModSettingNumberDefinition
                {
                    Key = "cooldown",
                    Label = "Cooldown (s)",
                    Description = "Seconds between activations.",
                    Path = "Settings/Combat",
                    DefaultValue = 2.0d,
                    MinValue = 0.5d,
                    MaxValue = 10.0d,
                    Step = 0.25d,
                    GetCurrentValue = () => _cooldown,
                    OnApply = value => _cooldown = value
                }
            ],

            // --- Choice (dropdown) ---
            ChoiceSettings =
            [
                new ModSettingChoiceDefinition
                {
                    Key = "mode",
                    Label = "Mode",
                    Description = "Controls the operating mode.",
                    Path = "Settings/Combat",
                    Options = ["Fast", "Balanced", "Slow"],
                    DefaultValue = "Fast",
                    GetCurrentValue = () => _mode,
                    OnApply = value => _mode = value
                }
            ],

            // --- Text (free-form LineEdit) ---
            TextSettings =
            [
                new ModSettingTextDefinition
                {
                    Key = "player_tag",
                    Label = "Player Tag",
                    Description = "Display name shown in the overlay.",
                    Path = "Settings/UI",
                    PlaceholderText = "Type a tag",
                    DefaultValue = "Player",
                    GetCurrentValue = () => _playerTag,
                    OnApply = value => _playerTag = value
                }
            ],

            // --- Color (text field accepting #RRGGBB, #RRGGBBAA, or r,g,b,a) ---
            ColorSettings =
            [
                new ModSettingColorDefinition
                {
                    Key = "accent_color",
                    Label = "Accent Color",
                    Description = "UI highlight color.",
                    Path = "Settings/UI",
                    PlaceholderText = "#50A8FFFF",
                    DefaultValue = "#50A8FFFF",
                    GetCurrentValue = () => _accentColor,
                    OnApply = value => _accentColor = value
                }
            ],

            // Called after the user presses Apply in the settings submenu.
            // Use this to react to the full set of values together (e.g. rebuild state).
            // If null, individual OnApply callbacks still fire per-setting.
            OnApply = null,

            // Called when the user presses Restore Defaults.
            // Reset all in-memory values back to their defaults here.
            OnRestoreDefaults = RestoreDefaults
        });

        // Hydrate from persisted values saved by a previous session.
        // Call this after Register() so the registry is ready.
        // It is a no-op if persistence is not yet ready (e.g. called before
        // the profile path is known) — use a Harmony postfix on the
        // profile-loaded event to call it again at the right time.
        TryHydrateFromPersistedValues();
    }

    public static void TryHydrateFromPersistedValues()
    {
        if (!ModSettingsRegistry.IsPersistenceReady())
        {
            return;
        }

        ModSettingsRegistry.RestorePersistedValues("MyMod");
    }

    private static void RestoreDefaults()
    {
        _featureEnabled = true;
        _cooldown = 2.0d;
        _mode = "Fast";
        _playerTag = "Player";
        _accentColor = "#50A8FFFF";
    }
}
```

**Path grouping** — the `Path` property on each setting definition controls where it appears in the tree view. Use `/` to create nested nodes:

| Path | Location in tree |
|---|---|
| `"Settings"` | Top-level Settings node |
| `"Settings/Combat"` | Settings → Combat |
| `"Settings/UI/Theme"` | Settings → UI → Theme |
| `"Advanced/Debug"` | Top-level Advanced → Debug |

**`AllowMultiplayerOverwrite`** — defaults to `true`. Set to `false` on client-only visual settings that cannot cause gameplay desync. When a multiplayer host applies settings, client settings with this flag `false` are left untouched.

**Hydration timing** — persistence is only ready after the player profile path is resolved. If your `Register()` call happens before that (which is typical), the first `TryHydrateFromPersistedValues()` will be a no-op. Add a Harmony postfix on the profile-loaded event (or mirror the pattern in `BetterRewards/src/Features/Settings/BetterRewardsSettingsHydrationPatch.cs`) to call hydration again at the right time.

See `src/Features/Examples/BuiltInExampleSettingsRegistration.cs` for a full working example of all setting types.

## License

MIT — see [LICENSE](LICENSE).
