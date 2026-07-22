# Enhanced Quests

Automates claiming and rerolling Daily and Weekly Quests while allowing each
supported quest type to be enabled or disabled independently.

## Configuration

Use Mod Helper to configure the general automation toggles and the Daily and
Weekly quest-type lists. Version 1.3.8 migrates the legacy config layout,
reloads external config changes and limits rerolls to candidates that match the
selected types and can be progressed on the current map.

The mod can claim completed quests, accept extra quest rewards, regenerate
empty sets, craft Whetstones for compatible temporary-item quests and retry
processing after quest, map and enemy-state changes. Safety guards prevent
overlapping claim or reroll operations.

## Install and build

Install the ZIP from the repository's Live Mod Files directory, or build
`Enhanced_Quests.csproj` with the .NET 9 SDK and an initialized Idle Slayer Mod
Manager. The produced assembly still targets .NET 6.

Original mod by Shokram and CrossTM. Version 1.3.8 improvements contributed by
Joeydm6.
