# Library belongs to https://github.com/legovader09/Idle-Slayer-Mods

# Core library for my idle slayer mods

## Getting started

In order to get started, simply download the mod from [Nexus](https://www.nexusmods.com/idleslayer/mods/3) or from [Nuget](https://www.nuget.org/packages/IdleSlayerMods.Common), then include it as a reference in your plugin project.

## Current features:

### Mod Helper

| class ModHelper : MonoBehaviour | Instantiated Mod Helper that gets injected into the Game scene                     |
|---------------------------------|------------------------------------------------------------------------------------|
| event ModHelperMounted          | Event that gets fired when the class has successfully been added to the Game scene |
| void ShowNotification()         | Display a message in the center of the screen, similar to an event message         |
| void ShowDialog()               | Show a customisable dialog popup message                                           |
| void ShowAchievement()          | Show a customisable achievement style popup                                        |
| void AddPanelButton()           | Adds a button to the info panel. The same place as Achievements, Options, etc.     |

### Achievement Helper

| class AchievementHelper             | Achievement helper class to manage achievements                           |
|-------------------------------------|---------------------------------------------------------------------------|
| static Achievement AddAchievement() | Instantiates a new Achievement scriptable object, and returns this object |

### BaseConfig:

| abstract class BaseConfig   | Base configuration class which can be inherited from       |
|-----------------------------|------------------------------------------------------------|
| abstract void SetBindings   | Takes in a ConfigFile and assigns it to the class instance |
| virtual ConfigEntry<T> Bind | Creates and assigns a new setting of type T                |

## Planned:
- Modded Achievement section in options
- Global list of modded achievements that can be appended to
- Simple UI button creation
- Custom random box events
- Custom plugin wrapper to make class injection and scene based component adding simpler

## Usage

The ModHelper class is **currently only accessible in the `Game` scene** of Idle Slayer.

In order to make use of the `ModHelper` behaviour object, you can either:
1. Make use of the `ModHelperMounted` event.
2. Find the `BepInEx` injected `GameObject` and use `GetComponent<ModHelper>()`.

### Using the ModHelperMounted event (Method 1)

In your `Plugin.cs`, you'll need to add a `static ModHelper` variable. This is so that your `MonoBehaviour` classes will have access to the helper, via the `Plugin` class.

You then need to create a event delegate for the event handler which will return the ModHelper instance that exists in the game.

#### See example:
```csharp
using IdleSlayerMods.Common;
public class Plugin : BasePlugin
{
    internal static ModHelper ModHelperInstance;
    
    public override void Load()
    {
        Log = base.Log;
        ..
        ModHelper.ModHelperMounted += SetModHelperInstance;
    }
    
    private static void SetModHelperInstance(ModHelper instance) => ModHelperInstance = instance;
}
```
To access within a MonoBehaviour script will then be as simple as:
```csharp
using UnityEngine;
public class CustomBehaviour : MonoBehaviour
{
    void Awake()
    {
        Plugin.ModHelperInstance.ShowNotification("Custom message!");
    }
}
```

### Using BaseConfig

Using the base config simplifies mod settings creation.

See [Using BaseConfig](./Config/README.md)
