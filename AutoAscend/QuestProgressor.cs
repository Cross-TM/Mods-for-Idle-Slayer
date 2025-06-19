using IdleSlayerMods.Common.Extensions;
using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using HarmonyLib;
using MelonLoader;

using System.IO;

namespace AutoAscendMod
{
    public class QuestProgressor : MonoBehaviour
    {
        public static QuestProgressor Instance { get; set; }

        ShopManager _shopManager;
        PlayerInventory _playerInventory;
        MapController _mapController;
        QuestManager _questManager;
        PlayerSkinManager _playerSkinManager;

        bool _portalPurchased;


        Il2CppSystem.Collections.Generic.List<Quest> questsScrollList;

        GameObject _questGameObject;

        private static readonly string[] ExcludedEnemyNames =
        [
            "enemy_frozen_souls",
            "enemy_soul_hobgoblin"
        ];

        private static bool IsSoulExclusion(Enemy e)
        {
            var n = e.name;
            return ExcludedEnemyNames.Contains(n)
                || n.StartsWith("enemy_soul_goblin");
        }



        public void Awake()
        {
            Instance = this;

            _shopManager = ShopManager.instance;
            _playerInventory = PlayerInventory.instance;
            _mapController = MapController.instance;
            _questManager = QuestManager.instance;
            _playerSkinManager = PlayerSkinManager.instance;

            _questGameObject = GameObject.Find("UIManager/Safe Zone/Shop Panel/Wrapper/Quests/Quests");

        }

        public void Start()
        {
            RefreshQuestsScrollList();
            RefreshQuestsScrollList();
            CheckQuestList();
        }

        private void LateUpdate()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                CheckQuestList();
            }

            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
//                LogAllUpgrades();
            }
            /*            if (Input.GetKeyDown(KeyCode.Alpha2))
                        {
                            Plugin.Logger.Msg("Spawning Chest Key");
                            Vector2 spawnPos = new(_mapController.player.position.x + 25f, _mapController.groundYPos);
                            _mapController.SpawnChestHuntKey(spawnPos);
                        }
            */
        }

        private void RefreshQuestsScrollList()
        {
            _questGameObject.GetComponent<QuestsList>().RefreshList();
            questsScrollList = _questGameObject.GetComponent<QuestsList>().lastScrollListData;
            
            _portalPurchased = AscensionSkills.list.Portals.unlocked;
        }

        public void CheckQuestList()
        {
            
//            Plugin.Logger.Msg("Checking Quest List...");

            if (!GameState.IsRunner()) return;

//            Plugin.Logger.Msg("GameState is Runner, proceeding with quest checks...");

            RefreshQuestsScrollList();

            if (_portalPurchased)
            {
                Quest targetQuest = null;
                foreach (var q in questsScrollList)
                {
                    if (q.unlocksUpgrade != null && q.unlocksUpgrade.name.EndsWith("_quests"))
                    {
                        targetQuest = q;
                        break;
                    }
                }

                BaseMap bestMap = null;

                if (targetQuest != null)
                {
                    var questMaps = BestMapForQuest(targetQuest, "Special");
                    bestMap = PickBestMap(questMaps);
                }

                if (bestMap == null)
                {
                    var allMaps = new List<BaseMap>();
                    foreach (var m in _mapController.GetAvailableMaps(true))
                    {
                        allMaps.Add(new BaseMap(m.Pointer));
                    }
                    bestMap = PickBestMap(allMaps);
                }

                if (bestMap != null)
                {
                    bool isCurrentMap = GameState.IsRunner()
                        ? bestMap == (_mapController.selectedMap as BaseMap)
                        : bestMap.Pointer == _mapController.lastRunnerMap.Pointer;

                    if (GameState.IsRunner())
                    {
                        if(!isCurrentMap)
                            _mapController.SpawnPortal(bestMap, new(new Vector2(_mapController.player.position.x + 25f, _mapController.groundYPos)));

                        SetCharacterSkin(bestMap);
                    }
                }
            }
        }
        private List<BaseMap> BestMapForQuest(Quest quest, String priority = "normal")
        {
            var matches = new List<BaseMap>();

            if (!QuestRequiresEnemyScan(quest))
            {
                return matches;
            }
            foreach (Map m in _mapController.GetAvailableMaps(true))
            {
                var map = new BaseMap(m.Pointer);
                if (MapMatchesQuest(map, quest))
                    matches.Add(map);

            }

            if (matches.Count == 0 && priority == "Special" && quest.questType != QuestType.KillEnemiesOfType)
            {
                Quest blocker;

                if (quest.questType == QuestType.KillEnemies)
                {
                    blocker = quest.enemyToKill.upgradeToUnlockIt.unlockedWithQuest;
                    matches = BestMapForQuest(blocker, "Special");
                }
            }

            return matches;
        }

        private BaseMap PickBestMap(List<BaseMap> maps)
        {
            BaseMap bestMap = null;
            int bestScore = -1;

            foreach (var map in maps)
            {
                int score = QuestsDoableInMap(map);

                bool isTieBreaker = GameState.IsRunner()
                    ? map == (_mapController.selectedMap as BaseMap)
                    : map.Pointer == _mapController.lastRunnerMap.Pointer;

                if (score > bestScore || (score == bestScore && isTieBreaker))
                {
                    bestScore = score;
                    bestMap = map;
                }
            }

            return bestMap;
        }

        private int QuestsDoableInMap(BaseMap map)
        {
            int count = 0;

            foreach (var quest in questsScrollList)
            {

                if (!QuestRequiresEnemyScan(quest))
                {
                    // everything else is auto-doable
                    count++;
                }
                else if (MapMatchesQuest(map, quest))
                {
                    // kill-type quests only count if the map has the right enemies
                    count++;
                }
            }

            return count;
        }

        private bool QuestRequiresEnemyScan(Quest quest)
        {
            switch (quest.questType)
            {
                case QuestType.KillEnemies when !IsSoulExclusion(quest.enemyToKill):
                case QuestType.KillEnemiesWithArrows when quest.enemyToKill.name != "enemy_king_goblin":
                case QuestType.KillEnemiesWithCharacter:
                case QuestType.KillEnemiesOfType:
                    return true;

                case QuestType.KillGiants:
                    return quest.enemyToKill != null;

                default:
                    return false;
            }
        }

        private bool MapMatchesQuest(BaseMap map, Quest quest)
        {
            map.SetCurrentStageEnemies();

            foreach (var e in map.currentStageEnemies)
            {
                // match by type
                if (quest.enemyType != null
                 && e.type != null
                 && e.type == quest.enemyType)
                    return true;

                // match by specific enemy or same-stage-or-below
                if (quest.enemyToKill != null
                 && (_questManager.EnemyFromSameStageOrBelow(quest.enemyToKill, e)))
                    return true;
            }
            return false;
        }

        private bool MapMatchesEnemy(BaseMap map, Enemy enemy)
        {
            map.SetCurrentStageEnemies();

            foreach (var e in map.currentStageEnemies)
            {
                // match by specific enemy or same-stage-or-below
                if (enemy != null
                 && (_questManager.EnemyFromSameStageOrBelow(enemy, e)))
                    return true;
            }
            return false;
        }

        private void SetCharacterSkin(BaseMap bestMap)
        {
            bool SkinSet = false;
            foreach (var q in questsScrollList)
            {
                if (q.questType == QuestType.KillEnemiesWithCharacter)
                {
                    bestMap.SetCurrentStageEnemies();

                    foreach (Enemy e in bestMap.currentStageEnemies)
                    {
                        if (_questManager.EnemyFromSameStageOrBelow(q.enemyToKill, e))
                        {
                            if (_playerSkinManager.skin != q.characterRequired)
                                _playerSkinManager.ApplySkin(q.characterRequired);

                            SkinSet = true;
                            break;
                        }
                    }
                }
                if (SkinSet) break;
            }

            if (!SkinSet && _playerSkinManager.skin != CharacterSkins.list.Agnis)
            {
                _playerSkinManager.ApplySkin(CharacterSkins.list.Agnis);
            }

        }

        private void LogAllQuests()
        {
            int i = 1;
            Plugin.Logger.Msg("-----------Start of Quest Log-----------");

            foreach (var quest in _playerInventory.allQuests)
            {
                var questTypeName = quest.GetIl2CppType().FullName;
                if (questTypeName == "DailyQuest" || questTypeName == "WeeklyQuest") continue; // Skip Daily and Weekly Quests

                Plugin.Logger.Msg("");
                Plugin.Logger.Msg($"--------------Quest {i}--------------");
                Plugin.Logger.Msg($"Quest Name: {quest.name}");
                Plugin.Logger.Msg($"Localized Name: {quest.localizedName}");
                Plugin.Logger.Msg($"Description: {quest.GetDescription()}");
                Plugin.Logger.Msg($"Quest Type: {quest.questType}");
                Plugin.Logger.Msg($"Enemy to Kill: {quest.enemyToKill?.localizedName ?? "None"}");
                Plugin.Logger.Msg($"Enemy name: {quest.enemyToKill?.name ?? "None"}");
                Plugin.Logger.Msg($"Enemy Type: {quest.enemyType?.localizedName ?? "None"}");
                Plugin.Logger.Msg($"Enemy Type name: {quest.enemyType?.name ?? "None"}");
                Plugin.Logger.Msg($"Quest Goal: {quest.questGoal}");
                Plugin.Logger.Msg($"Upgrade Unlocked: {quest.unlocksUpgrade?.name ?? "None"}");
                Plugin.Logger.Msg($"Upgrade Description: {quest.unlocksUpgrade?.GetDescription() ?? "None"}");
                Plugin.Logger.Msg($"Character Needed: {quest.characterRequired?.localizedName ?? "None"}");
                Plugin.Logger.Msg($"Is Claimed: {quest.isClaimed}");
                Plugin.Logger.Msg($"-----------------------------------");

                i++;
            }
        }

        private void LogAllUpgrades()
        {
            var path = Path.Combine(Application.persistentDataPath, "upgrades.csv");
            var sb = new StringBuilder();

            // header
            sb.AppendLine("Index,Name,LocalizedName,Description,Cost,Bought,Disabled,IsSpecialUnlock,SpecialUnlocked,PermanentUpgrade,UnlockedWithQuest,MaterialName");

            int i = 1;
            foreach (var u in _playerInventory.upgrades)
            {
                // safe-call GetDescription
                string rawDesc;
                try
                {
                    rawDesc = u.GetDescription();
                }
                catch (FormatException fe)
                {
                    Plugin.Logger.Warning($"[GetDescription] bad format on '{u.name}': {fe.Message}");
                    rawDesc = u.localizedDescription ?? "";
                }
                catch (Exception ex)
                {
                    Plugin.Logger.Warning($"[GetDescription] exception on '{u.name}': {ex.GetType().Name}");
                    rawDesc = u.localizedDescription ?? "";
                }

                // escape helper
                string esc(string s) => (s ?? "").Replace("\"", "\"\"");

                var name = esc(u.name);
                var locName = esc(u.localizedName);
                var desc = esc(rawDesc);
                var cost = u.GetCost();
                var bought = u.bought.ToString().ToLower();
                var disabled = u.disabled.ToString().ToLower();
                var spec = u.isSpecialUnlock.ToString().ToLower();
                var unlocked = u.specialUnlocked.ToString().ToLower();
                var perm = u.permanentUpgrade.ToString().ToLower();
                var quest = esc(u.unlockedWithQuest?.name);
                var mat = esc(u.GetMaterial()?.name);

                sb.AppendLine(
                    $"{i},\"{name}\",\"{locName}\",\"{desc}\",{cost},{bought},{disabled},{spec},{unlocked},{perm},\"{quest}\",\"{mat}\""
                );
                i++;
            }

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            Plugin.Logger.Msg($"[CSV] Exported upgrades → {path}");
        }



        [HarmonyPatch(typeof(Quest), "Claim")]
        public static class Quest_Claim_Patch
        {
            public static void Postfix()
            {
                QuestProgressor.Instance?.Invoke(
                    nameof(QuestProgressor.Instance.CheckQuestList), 2f);
            }
        }

        [HarmonyPatch(typeof(MapController), "ChangeMap")]
        public class Patch_MapControllerChangeMap
        {
            static void Postfix()
            {
                QuestProgressor.Instance?.Invoke(
                    nameof(QuestProgressor.Instance.CheckQuestList), 2f);
            }
        }
    }
}