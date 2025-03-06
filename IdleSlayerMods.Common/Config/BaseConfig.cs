using BepInEx.Configuration;

namespace IdleSlayerMods.Common.Config;

public abstract class BaseConfig
{
    private readonly ConfigFile _cfg;

    protected BaseConfig(ConfigFile cfg)
    {
        _cfg = cfg;
        SetBindings();
    }
    
    protected abstract void SetBindings();

    protected virtual ConfigEntry<T>? Bind<T>(string section, string key, T defaultValue, string description = "") =>
        _cfg.Bind(
            section: section,
            key: key,
            defaultValue: defaultValue,
            configDescription: new(
                description
            )
        );
    
    protected virtual ConfigEntry<int>? Bind(string section, string key, int defaultValue, string description = "", int minValue = 0, int maxValue = 100) =>
        _cfg.Bind(
            section: section,
            key: key,
            defaultValue: defaultValue,
            configDescription: new(
                description, new AcceptableValueRange<int>(minValue, maxValue)
            )
        );
}
