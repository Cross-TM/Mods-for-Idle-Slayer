using IdleSlayerMods.Common.Extensions;
using Il2Cpp;
using MelonLoader;
using System.Collections;
using UnityEngine;
using HarmonyLib;

namespace AutoAscendMod
{

    public class AutoAscend : MonoBehaviour
    {
        public static AutoAscend Instance { get; private set; }

        ShopManager _shopManager;
        PlayerInventory _playerInventory;
        AscensionManager _ascensionManager;

        GameObject _questGameObject;
        GameObject _powerGameObject;
        GameObject _upgradeGameObject;

        Il2CppSystem.Collections.Generic.List<Quest> questsScrollList;
        Il2CppSystem.Collections.Generic.List<Power> powersScrollList;
        Il2CppSystem.Collections.Generic.List<Upgrade> upgradesScrollList;

        private bool purchaserRunning = false;
        private bool ascendReady = false;
        private bool questsChecking = false;

        Power[] _allPowers;
        Power Sword;        //  equipment_1
        Power Shield;       //  equipment_2
        Power Armor;        //  equipment_3
        Power Helmet;       //  equipment_4
        Power Boots;        //  equipment_5
        Power Ring;         //  equipment_6
        Power Dagger;       //  equipment_7
        Power Axe;          //  equipment_8
        Power Staff;        //  equipment_9
        Power Bow;          //  equipment_10
        Power Spellbook;    //  equipment_11
        Power Spirit;       //  equipment_12
        Power Necklace;     //  equipment_13
        Power Gloves;       //  equipment_14
        Power Cape;         //  equipment_15
        Power Claw;         //  equipment_16
        Power Spear;        //  equipment_17
        Power Shuriken;     //  equipment_18

        public void Awake()
        {
            Instance = this;
            _shopManager = ShopManager.instance;
            _playerInventory = PlayerInventory.instance;
            _ascensionManager = AscensionManager.instance;

            _allPowers = _playerInventory.powers;

            _questGameObject = GameObject.Find("UIManager/Safe Zone/Shop Panel/Wrapper/Quests/Quests");
            _powerGameObject = GameObject.Find("UIManager/Safe Zone/Shop Panel/Wrapper/Powers/Powers");
            _upgradeGameObject = GameObject.Find("UIManager/Safe Zone/Shop Panel/Wrapper/Upgrades/Upgrades");
            
        }

        public void Start()
        {
            InitialisePowers();

            RefreshScrollList("Quests");
            RefreshScrollList("Powers");
            RefreshScrollList("Upgrades");

        }

        private void InitialisePowers()
        {
            Sword = _allPowers[0];
            Shield = _allPowers[1];
            Armor = _allPowers[2];
            Helmet = _allPowers[3];
            Boots = _allPowers[4];
            Ring = _allPowers[5];
            Dagger = _allPowers[6];
            Axe = _allPowers[7];
            Staff = _allPowers[8];
            Bow = _allPowers[9];
            Spellbook = _allPowers[10];
            Spirit = _allPowers[11];
            Necklace = _allPowers[12];
            Gloves = _allPowers[13];
            Cape = _allPowers[14];
            Claw = _allPowers[15];
            Spear = _allPowers[16];
            Shuriken = _allPowers[17];
        }

        private void RefreshScrollList(String panel)
        {
            switch (panel)
            {
                case "Quests":
                    _questGameObject.GetComponent<QuestsList>().RefreshList();
                    questsScrollList = _questGameObject.GetComponent<QuestsList>().lastScrollListData;
                    break;
                case "Powers":
                    _powerGameObject.GetComponent<PowersList>().RefreshList();
                    powersScrollList = _powerGameObject.GetComponent<PowersList>().lastScrollListData;
                    break;
                case "Upgrades":
                    _upgradeGameObject.GetComponent<UpgradesList>().RefreshList();
                    upgradesScrollList = _upgradeGameObject.GetComponent<UpgradesList>().lastScrollListData;
                    break;
            }
        }

        public void LateUpdate()
        {
            if (!ascendReady)
            {
                MelonCoroutines.Start(AscendAndPurchase());
            }

            if (!purchaserRunning)
            {
                purchaserRunning = true;
                MelonCoroutines.Start(RunPurchaser());
            }
/*            if (!questsChecking)
            {
                questsChecking = true;
                CheckQuests();
            }
*/
//            KeyPresses();
        }

/*        private void CheckQuests()
        {
            RefreshScrollList("Quests");

            if (questsScrollList != null && questsScrollList.Count > 0)
            {
                foreach (Quest quest in questsScrollList)
                {
                    if (quest.CanBeClaimed())
                    {
                        quest.Claim();
                        RefreshScrollList("Quests");
                    }
                }
            }
            questsChecking = false;
        }
*/
        private IEnumerator AscendAndPurchase()
        {
            ascendReady = true;
            if (SlayerPoints.pre >= SlayerPoints.lifetime * 0.2 && SlayerPoints.pre > 25 &&
                GameState.IsRunner())   // && RageModeManager.instance?.currentState == RageModeManager.RageModeStates.NotActive

            {
                yield return new WaitForSeconds(60f);
                _ascensionManager.Ascend();
                for (int i = 0; i < 10; i++)
                {
                    _ascensionManager.BuyAllAction();
                }
            }
            ascendReady = false;
        }

        private IEnumerator RunPurchaser()
        {
            RefreshScrollList("Upgrades");


            foreach (Upgrade upgrade in upgradesScrollList)
            {
                if (upgrade.GetCost() < _playerInventory.coins)
                {
                    _shopManager.BuyUpgrade(upgrade);
                }
                else
                    break;
            }

            yield return new WaitForSeconds(2f);

            double cost = 0;

            RefreshScrollList("Powers");


            powersScrollList.Reverse();

            for (int i = 0; i < powersScrollList.Count; i++)
            {
                Power power = powersScrollList[i];

                if (i >= 0 && i < 4 && power.level < 25)
                {
                    cost = power.CalculateMaxCost().cost;
                    if (cost > _playerInventory.coins)
                    {
                        continue;
                    }
                    else
                    {
                        _shopManager.SelectStack(-1);
                        _shopManager.BuyPower(power);
                    }
                }
                else if (i >= 0 && i < 4 && power.level < 150)
                {
                    int currentLevel = power.level;
                    int diff = 10 - (currentLevel % 10);

                    cost = power.CalculateCost(diff);

                    while (cost < _playerInventory.coins) 
                    {
                        _shopManager.SelectStack(10);
                        _shopManager.BuyPower(power);
                        cost = power.CalculateCost(10);
                    }
                }
                else 
                {
                    int currentLevel = power.level;
                    int diff = 50 - (currentLevel % 50);

                    cost = power.CalculateCost(diff);

                    while (cost < _playerInventory.coins)
                    {
                        _shopManager.SelectStack(50);
                        _shopManager.BuyPower(power);
                        cost = power.CalculateCost(50);
                    }
                }
            }

            RefreshScrollList("Powers");

            yield return new WaitForSeconds(2f);

            purchaserRunning = false;

        }


        private void KeyPresses()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                _shopManager.BuyPower(Sword);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                _shopManager.BuyPower(Shield);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                _shopManager.BuyPower(Armor);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                _shopManager.SelectStack(1);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                _shopManager.SelectStack(10);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha6))
            {
                _shopManager.SelectStack(50);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha7))
            {
                _shopManager.SelectStack(-1);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha8))
            {
                _shopManager.BuyAllUpgrades();
            }
        }
    }
}
