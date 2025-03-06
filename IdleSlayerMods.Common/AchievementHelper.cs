using UnityEngine;

namespace IdleSlayerMods.Common;

public class AchievementHelper
{
    /// <summary>
    /// Creates a new achievement.
    /// <seealso cref="Achievement"/>
    /// </summary>
    /// <param name="type">The type of this achievement. See <see cref="AchievementType"/>.</param>
    /// <param name="achievementName">The name of the achievement.</param>
    /// <param name="description">The achievement description. Usually details the unlock conditions.</param>
    /// <param name="icon">Achievement icon to display.</param>
    /// <param name="grantsProgressionPoint">Whether this achievement should count towards progress and bonuses.</param>
    /// <returns>A new instance of the <see cref="Achievement"/> object.</returns>
    public static Achievement CreateAchievement(AchievementType type, string achievementName, string description, Sprite? icon = null, bool grantsProgressionPoint = false)
    {
        var achievement = ScriptableObject.CreateInstance<Achievement>();
        achievement.id = $"{type}_{achievementName}";
        achievement.achievementType = type;
        achievement.description = description;
        achievement.excludeFromSteam = true;
        achievement.name = achievementName;
        achievement.icon = icon;
        achievement.grantsProgressionPoint = grantsProgressionPoint;
        achievement.localizedDescription = description;
        achievement.localizedName = achievementName;
        achievement.unlocked = false;
        return achievement;
    }
}