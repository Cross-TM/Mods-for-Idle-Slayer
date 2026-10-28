### Version 2.1.0

- Perfect mode now opens chests in resource-maximising order: Duplicate Next Pick, then (Armory chests if maximised), Multiplier Increaser, Multipliers (best first), resource chests (Both, CPS, Slayer Points), then the rest. Duplicate bonuses now land on the Multiplier Increaser instead of being spent on Armory chests or the best Multiplier
- Debug builds open chests in the same order as Release builds
- Debug log shows the multiplier of Multiplier chests

### Version 2.0.1

- Fixed NullReferenceException spam in `ChestKiller.Update()` after the October 2026 game update (#10)
- Fixed Perfect mode opening Mimics: achievements are now looked up once loaded instead of in `Awake`, when they are still empty
- If the kill Mimic achievement cannot be found, it is treated as unlocked so Mimics are never opened on purpose

### Version 1.1.0

- Added config option to reveal duplicator item
- Internal renames to "Chest Revealer"

### Version 1.0.3

- Updated for new config format in core library

### Version 1.0.2

- Updated to reflect core mod dependency

### Version 1.0.1

- Added config option to reveal multipliers
- Better logic to prevent performance expensive checks

### Version 1.0.0

Initial release