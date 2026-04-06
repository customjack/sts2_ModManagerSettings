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

```csharp
ModSettingsRegistry.UpsertRegistration(new ModSettingsRegistration
{
    ModPckName = "MyMod",
    DisplayName = "My Mod",
    Description = "Settings for My Mod.",
    ToggleSettings = new[]
    {
        new ModSettingToggleDefinition
        {
            Key = "my_toggle",
            Label = "Enable Feature",
            Description = "Turns on the feature.",
            Path = "Settings",
            DefaultValue = true,
            GetCurrentValue = () => MySettings.EnableFeature,
            OnApply = value => MySettings.EnableFeature = value
        }
    }
});
```

See `src/Features/Examples/BuiltInExampleSettingsRegistration.cs` for a full example of all setting types.

## License

MIT — see [LICENSE](LICENSE).
