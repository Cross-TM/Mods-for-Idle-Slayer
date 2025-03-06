# Using BaseConfig

In order to utilise `BaseConfig`, we must first create a class that inherits from `BaseConfig`.

Note that `BaseConfig` requires a `ConfigFile` to be passed down into it, so we will do exactly that directly from the constructor:
`public sealed class Settings(ConfigFile cfg) : BaseConfig(cfg)`.

Since `SetBindings()` is **abstract**, it means we must now implement it ourselves **in the derived class**. This is where you will create the settings for your mod. See the example below:

```csharp
using IdleSlayerMods.Common.Config;

public sealed class Settings(ConfigFile cfg) : BaseConfig(cfg)
{
    internal ConfigEntry<KeyCode> ExampleInputKey;

    protected override void SetBindings()
    {
        ExampleInputKey = Bind("General", "ExampleInputKey", KeyCode.B,
            "The key bind for my mod");
    }
}
```

Finally, to be able to use the settings in your mod, you will need to add the following in your `Plugin.cs Load()` method:

```csharp

public class Plugin : BasePlugin
{
    internal new static ManualLogSource Log;
    ..
    internal static Settings Settings;
    ..

    public override void Load()
    {
        Log = base.Log;
        ..
        Settings = new(Config)
    }
}
```
`Config` comes from `BasePlugin`. 

In this example, I also make the Settings variable static, this is so that any `MonoBehaviour` scripts can easily access the settings, just like how the log is accessible.

### Example usage:

```csharp
using UnityEngine;

public class ExampleBehaviour : MonoBehaviour
{
    ..

    private void Update()
    {
        if (Input.GetKeyDown(Plugin.Settings.ExampleInputKey.Value))
        {
            // Do some cool actions
        }
    }
}
```