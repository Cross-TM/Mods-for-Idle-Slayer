using Il2Cpp;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HawkTuah
{
    public class HawkTuahRename : MonoBehaviour
    {
        public static HawkTuahRename Instance { get; private set; }

        PlayerInventory playerInventory;
        Upgrade hawkeye;


        public void Awake()
        {
            playerInventory = PlayerInventory.instance;

            foreach (var upgrade in playerInventory.upgrades)
            {
                if (upgrade.name == "upgrade_hawk_eye")
                {
                    hawkeye = upgrade;
                    break;
                }
            }

        }

        public void Update()
        {
            if (hawkeye != null)
            { 
                if (hawkeye.localizedName == "Hawk Eye")
                {
                    hawkeye.localizedName = "Hawk Tuah";
                }
            }
        }
    }
}