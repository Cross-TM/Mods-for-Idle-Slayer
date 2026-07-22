# Armory Manager

Automatically evaluates and breaks low-value Armory items while protecting
equipped, reserved and strategically useful equipment.

## Configuration

Automation installs disabled. Configure the maximum level eligible for
breaking, gear types that must never be broken, the minimum number of items to
keep and the advanced equip-requirement calculation before enabling it.

Version 2.3.9 separates scanning, evaluation, protection, breaking and visuals
into testable components. The UI highlights reserved items and the automation
rechecks live Armory state immediately before every destructive action.

## Install, build and test

Build `ArmoryManager.csproj` with the .NET 9 SDK and an initialized Idle Slayer
Mod Manager. The produced assembly still targets .NET 6. Run the xUnit suite
with:

```powershell
dotnet test .\ArmoryManager.Tests\ArmoryManager.Tests.csproj -c Release
```

Original mod by CrossTM, previously patched by nicholasptran. Version 2.3.9
improvements contributed by Joeydm6.
