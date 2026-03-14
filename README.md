# ModManagerSettings

`ModManagerSettings` is a shared settings-host mod for Slay the Spire 2.

It adds a settings button per mod in the in-game mod list and exposes an API so other mods can register settings definitions, defaults, and apply/reset callbacks.

## Dependencies

- None required for runtime.
- Other mods can depend on `ModManagerSettings` to host their settings UI.

## Install (Manual)

1. Close Slay the Spire 2.
2. Extract the release zip for `ModManagerSettings`.
3. In Steam, right-click `Slay the Spire 2` -> `Properties` -> `Installed Files` -> `Browse`.
4. In the game folder that opens, create a `mods` folder if it does not exist.
5. Drag the extracted `ModManagerSettings` folder into `mods`.
6. Confirm these files exist in `mods/ModManagerSettings`:
   - `ModManagerSettings.dll`
   - `ModManagerSettings.pck`
7. Launch Slay the Spire 2. If prompted to enable mods, accept and relaunch.
8. In-game, open `Settings` -> `General` -> `Mods` and make sure `ModManagerSettings` is enabled.

## Developer Notes

- Build (WSL/Linux scripts):
  - `./scripts/bash/build_and_stage.sh`
  - `./scripts/bash/make_pck.sh`
  - `./scripts/bash/install_to_game.sh`
- Environment:
  - Copy `.env.example` to `.env`
  - Set `STS2_INSTALL_DIR` in `.env`
