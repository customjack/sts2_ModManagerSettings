# ModManagerSettings

`ModManagerSettings` is a clean starter project for building a shared settings host mod for STS2.

This project was copied from `ExampleMod` and stripped of all tutorial gameplay features so we can implement:
- mod-menu settings-button injection per installed mod
- a registration API for other mods to declare settings rows
- callbacks for change/apply/default/reset behavior

## Current status

- Project renamed to `ModManagerSettings`.
- Example gameplay patches removed.
- Bootstrap + build/pack/install scripts retained.

## Quick start

1. Create local env file:
   - `cp .env.example .env`
2. Set `STS2_INSTALL_DIR` in `.env`.
3. Build and stage:
   - `./scripts/bash/build_and_stage.sh`
4. Build pck:
   - `./scripts/bash/make_pck.sh`
5. Install:
   - `./scripts/bash/install_to_game.sh`
