using Il2Cpp;
using UnityEngine;
using HarmonyLib;

namespace BonusStageQuestCompleter;

public class CompleteGoblinQuest : MonoBehaviour
{
    public static CompleteGoblinQuest Instance { get; private set; }
    private Il2CppSystem.Collections.Generic.List<Quest> questList;

    public void Awake()
    {
        Instance = this;
    }

    public void LateUpdate()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            CompleteHyperModeQuest();
        }
    }

    private void CompleteHyperModeQuest()
    {
        questList = QuestsList.instance?.lastScrollListData;

        foreach (Quest quest in questList)
        {
            if (quest.name == "quest_hyper_mode")
            {
                quest.AddProgress(quest.questGoal - quest.questCurrentGoal);
                Plugin.Logger.Msg("Hyper Mode Quest Completed");
                return;
            }
        }
    }
}
