# Auto Jump

Automatically jumps over supported enemies and obstacles and can optionally
activate Boost during normal runner gameplay.

## Configuration

- `UseAutoJump` enables automatic jumping.
- `AutoJumpToggleKey` changes the toggle key (`Z` by default).
- `UseAutoBoost` enables automatic Boost.
- `AutoBoostToggleKey` changes the Boost toggle key (`B` by default).

The mod pauses or restores movement state around supported minigames and other
mods that temporarily control the player. Version 1.0.6 also improves reflected
mod-state lookup and restores arrow and movement state more safely.

## Install and build

Install the ZIP from the repository's Live Mod Files directory, or build
`AutoJumpMod.csproj` with the .NET 9 SDK and an initialized Idle Slayer Mod
Manager. The produced assembly still targets .NET 6.
The optional soak helper is available under `tools/Verify-AutoJumpSoak.ps1`.

Original mod by CrossTM. Version 1.0.6 improvements contributed by Joeydm6.
