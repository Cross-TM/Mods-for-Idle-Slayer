using Il2Cpp;
using MelonLoader;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using HarmonyLib;
using System.Reflection;
using UnityEngine.UI;

namespace AutoJumpMod
{
    // Helper to query other mods via reflection
    internal class ModFlagChecker
    {
        private readonly string _qualifiedTypeName;
        private readonly string _privateFieldName;
        private System.Type _type;
        private FieldInfo _field;
        private bool _initialized;

        public ModFlagChecker(string qualifiedTypeName, string privateFieldName = "")
        {
            _qualifiedTypeName = qualifiedTypeName;
            _privateFieldName = privateFieldName;
        }

        private void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            _type = System.Type.GetType(_qualifiedTypeName);
            if (_type == null)
            {
//                Plugin.Logger.Error($"Type not found: {_qualifiedTypeName}");
                return;
            }

            if (string.IsNullOrEmpty(_privateFieldName)) return;

            _field = _type.GetField(
                _privateFieldName,
                BindingFlags.NonPublic | BindingFlags.Instance
            );
            if (_field == null)
                Plugin.Logger.Error($"Field '{_privateFieldName}' not found on {_type.FullName}");
        }

        private UnityEngine.Object FindInstance()
        {
            Initialize();
            if (_type == null) return null;

            var findMethod = typeof(UnityEngine.Object)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m =>
                    m.Name == "FindObjectOfType"
                    && m.IsGenericMethodDefinition
                    && m.GetParameters().Length == 0
                );
            if (findMethod == null)
            {
                Plugin.Logger.Error("Couldn't find generic FindObjectOfType<>()");
                return null;
            }

            var generic = findMethod.MakeGenericMethod(_type);
            return generic.Invoke(null, System.Array.Empty<object>()) as UnityEngine.Object;
        }

        public bool IsLoaded() => FindInstance() != null;

        public bool GetBoolFlag()
        {
            var inst = FindInstance();
            if (inst == null || _field == null) return false;
            return (bool)_field.GetValue(inst);
        }
    }

    public class AutoJump : MonoBehaviour
    {
        private const float JumpSpotXRatio = 0.8f;
        private const string SkillBootsName = "ascension_upgrade_climbing_boots";
        private const string SkillBonusStage2Name = "map_bonus_stage_2";
        private const string SkillBonusGaps2Name = "ascension_upgrade_protect";
        private const string SkillBonusStage3Name = "map_bonus_stage_3";
        private const string SkillBonusGaps3Name = "ascension_upgrade_board_the_platforms";
        private const string SkillBowPurchasedName = "ascension_upgrade_sacred_book_of_projectiles";
        private const string SkillBowBoostName = "ascension_upgrade_stability";

        public static float OriginalArrowSpeed = -1f;
        public static float OriginalElectroSpeed = -1f;
        public static float NewArrowSpeed = -1f;
        public static float NewElectroSpeed = -1f;
        private static readonly List<Arrow> _activeArrows = new List<Arrow>();


        public static AutoJump Instance { get; private set; }
        public bool BootsPurchased => _bootsPurchased;

        private bool _prevBootsUnlocked;
        private bool _bootsUnlockHandled;
        private bool _autoJump;
        private bool _isJumping;
        private bool _isShooting;
        private bool _didStage3Delay;
        private bool _isAttacking;
        private bool _isBreaking;

        private bool _wasClockVisibleLastFrame;
        private bool _bootsPurchased;
        private bool _bootsChanged;
        private bool dummy;
        private bool _prevWindDashActive;
        private bool _bonusSection3Completed;

        private float dualChance;
        private int _bonusSection;

        // reflection-based mod checks
        private readonly ModFlagChecker _bscChecker = new(
            "BonusStageCompleter.BonusStageCompleter, BonusStageCompleter",
            "_skipAtSpiritBoostEnabled"
        );
        private readonly ModFlagChecker _autoBoostChecker = new(
            "AutoBoost.AutoBoost, AutoBoost",
            "_windDashEnabled"
        );
        private readonly ModFlagChecker _armoryManagerChecker = new(
            "ArmoryManager.BreakWeapons, ArmoryManager"
        );

        private Boost _boost;
        private JumpPanel _jumpPanel;
        private PlayerMovement _pm;
        private PointerEventData _jumpSpot;
        private RageModeManager _rageModeManager;
        private WindDash _windDash;
        private MapController _mapCtrl;
        private BonusMapController _bonusMapCtrl;
        private PlayerInventory _playerInventory;
        private WeaponsManager _weaponsManager;

        private AscensionSkill _bootsSkill;
        private AscensionSkill _bonusStage2Skill;
        private AscensionSkill _bonusGaps2Skill;
        private AscensionSkill _bonusStage3Skill;
        private AscensionSkill _bonusGaps3Skill;
        private AscensionSkill _bowPurchasedSkill;
        private AscensionSkill _bowBoostSkill;
        private RandomEvent dual;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            _jumpPanel = JumpPanel.instance;
            _pm = PlayerMovement.instance;
            _rageModeManager = RageModeManager.instance;
            _mapCtrl = MapController.instance;
            _bonusMapCtrl = BonusMapController.instance;
            _playerInventory = PlayerInventory.instance;
            _windDash = AbilitiesManager.instance.windDash;
            _boost = AbilitiesManager.instance.boost;
            _weaponsManager = WeaponsManager.instance;
            _autoJump = Plugin.Config.UseAutoJump.Value;
        }

        void Start()
        {
            if (EventSystem.current == null)
                Plugin.DLog("An EventSystem is required.");

            _jumpSpot = new PointerEventData(EventSystem.current)
            {
                position = new Vector2(Screen.width * JumpSpotXRatio, Screen.height)
            };

            InitializeSkills();

            _prevBootsUnlocked = _bootsSkill.unlocked;

            var REM = RandomEventManager.instance.randomEvents;
            if (REM != null)
            {
                foreach (var re in REM)
                {
                    if (re.name == "Dual Randomness")
                    {
                        dual = re;
                        dualChance = re.chance;
                        break;
                    }
                }
            }
        }

        void LateUpdate()
        {
            _activeArrows.RemoveAll(a => a == null || a.outOfCamera);

            HandleArrowSpeedOnWindDash();
            ResetStateOnRunner();
            DetectClockFlips();
            HandleBootsLogic();
            HandleAutoJump();
            MiniArmoryManager();
#if DEBUG
        HandleMapKeys();
#endif
        }

        // speeds up all on-screen arrows when windDash just activated
        private void HandleArrowSpeedOnWindDash()
        {
            bool windActive = IsWindDashEnabled() && _windDash.GetCooldown() < 1;
            if (windActive && !_prevWindDashActive)
            {
                foreach (var arrow in _activeArrows)
                    ChangeArrowSpeed(arrow);
            }
            _prevWindDashActive = windActive;
        }


        public bool IsBscLoaded() => _bscChecker.IsLoaded();
        public bool ShouldSkipAtSpiritBoost() => _bscChecker.GetBoolFlag();
        public bool IsAutoBoostLoaded() => _autoBoostChecker.IsLoaded();
        public bool IsWindDashEnabled() => _autoBoostChecker.GetBoolFlag();
        public bool IsArmoryManagerLoaded() => _armoryManagerChecker.IsLoaded();


        void OnDestroy() => Instance = Instance == this ? null : Instance;

        private void InitializeSkills()
        {
            foreach (AscensionSkill skill in _playerInventory.ascensionSkills)
            {
                switch (skill.name)
                {
                    case SkillBootsName:
                        _bootsSkill = skill;
                        if (_bootsSkill.unlocked) _bootsPurchased = true;
                        break;

                    case SkillBonusStage2Name:
                        _bonusStage2Skill = skill;
                        break;

                    case SkillBonusGaps2Name:
                        _bonusGaps2Skill = skill;
                        break;

                    case SkillBonusStage3Name:
                        _bonusStage3Skill = skill;
                        break;

                    case SkillBonusGaps3Name:
                        _bonusGaps3Skill = skill;
                        break;

                    case SkillBowPurchasedName:
                        _bowPurchasedSkill = skill;
                        break;

                    case SkillBowBoostName:
                        _bowBoostSkill = skill;
                        break;
                }
            }
        }
        private void ResetStateOnRunner()
        {
            if (!GameState.IsRunner()) return;
            _bonusSection = 0;
            _bonusSection3Completed = false;
        }

        private void DetectClockFlips()
        {
            bool showTime = _bonusMapCtrl.showCurrentTime;

            bool inStage1 = GameState.IsBonus()
                            && _mapCtrl.CurrentBonusMap() == Maps.list.BonusStage;
            bool inStage3 = GameState.IsBonus()
                            && _mapCtrl.CurrentBonusMap() == Maps.list.BonusStage3;

            if ((inStage1 || inStage3) && !_wasClockVisibleLastFrame && showTime && _bonusMapCtrl.currentSectionIndex == _bonusSection)
                _bonusSection++;
            _wasClockVisibleLastFrame = showTime;
        }

        private void HandleBootsLogic()
        {
            if (!_bootsUnlockHandled)
            {
                bool nowUnlocked = _bootsSkill.unlocked;
                if (!_prevBootsUnlocked && nowUnlocked)
                {
                    _bootsUnlockHandled = true;
                    _bootsPurchased = true;
                }
                _prevBootsUnlocked = nowUnlocked;
            }

            bool inStage3 = GameState.IsBonus()
                            && _mapCtrl.CurrentBonusMap() == Maps.list.BonusStage3;
            bool inStage2 = GameState.IsBonus()
                            && _mapCtrl.CurrentBonusMap() == Maps.list.BonusStage2;

            if (inStage3)
            {
                if (_bonusSection == 3 && !_bonusSection3Completed)
                {
                    _bootsSkill.unlocked = _pm.isMoving;
                }
                else
                {
                    if (_bonusMapCtrl.showCurrentTime)
                        _bootsSkill.unlocked = true;
                }

                if (dual != null)
                {
                    if (_bonusSection == 2)
                        dual.chance = 0f;
                    else
                        dual.chance = dualChance;
                }

                _bootsChanged = true;
            }
            else if (inStage2 && _bootsPurchased)
            {
                _bootsSkill.unlocked = _bonusMapCtrl.showCurrentTime;
                _bootsChanged = true;
            }
            else if (_bootsChanged && GameState.IsRunner() && _bootsPurchased)
            {
                _bootsChanged = false;
                _bootsSkill.unlocked = true;
            }
        }

        private void HandleAutoJump()
        {
            if (Input.GetKeyDown(Plugin.Config.AutoJumpToggleKey.Value))
            {
                _autoJump = !_autoJump;
                Plugin.Logger.Msg($"AutoJump is: {(_autoJump ? "ON" : "OFF")} ");
                Plugin.ModHelperInstance.ShowNotification(
                    _autoJump ? "Auto Jump enabled!" : "Auto Jump disabled!",
                    _autoJump
                );
                Plugin.Config.UseAutoJump.Value = _autoJump;
            }

            if (!_autoJump) return;

            bool inStage1 = GameState.IsBonus()
                            && _mapCtrl.CurrentBonusMap() == Maps.list.BonusStage;
            bool inStage3 = GameState.IsBonus()
                            && _mapCtrl.CurrentBonusMap() == Maps.list.BonusStage3;

            if (!inStage3 || _bonusSection != 2)
                _didStage3Delay = false;

            if (GameState.IsBonus()
                && !_isJumping
                && IsBscLoaded()
                && (ShouldSkipAtSpiritBoost() || !_bonusMapCtrl.spiritBoostEnabled))
            {

                _isJumping = true;
                if (inStage1 && _pm.IsGrounded() && _bonusSection == 1 && _bonusMapCtrl.showCurrentTime)
                {
                    MelonCoroutines.Start(LargeSingleJump());
                }
                else if (inStage3 && _bonusSection == 2 && !_didStage3Delay)
                {
                    _didStage3Delay = true;
                    MelonCoroutines.Start(Stage3DelayAndJump());
                }
                else
                    ShortSingleJump();
            }
            else if (CanJumpRunner())
            {
                _isJumping = true;
                ShortSingleJump();
            }

            if (GameState.IsRunner() && CanShoot())
            {
                if (_pm.IsGrounded())
                    _pm.ShootArrow();

                if (!_isShooting)
                {
                    _isShooting = true;
                    MelonCoroutines.Start(ShootArrows());
                }
            }

            //If attacking something with the sword
            if (GameState.IsRunner() && !_pm.isMoving && !_isAttacking)
            {
                _isAttacking = true;
                MelonCoroutines.Start(AttackGiant());
            }
        }

        private void MiniArmoryManager()
        {
            if (!IsArmoryManagerLoaded())
            {
                if (!_isBreaking && !_weaponsManager.hasFreeSlot && GameState.IsRunner())
                {
                    MelonCoroutines.Start(BreakLastWeapon());
                }
            }
        }

        private IEnumerator BreakLastWeapon() { 
            if (_weaponsManager == null) yield return null;

            if (!_weaponsManager.hasFreeSlot) 
            {
                _isBreaking = true;
                var list = _weaponsManager.currentItems;

                int before = list.Count;
                var toBreak = list[list.Count - 1];

                // 2) show the break‑popup
                _weaponsManager.BreakPopup(toBreak);

                // 3) click “Confirm” when it comes up
                yield return AutoConfirmBreak();

                // 4) wait until the item has actually left the list
                yield return new WaitUntil(new System.Func<bool>(() => list.Count < before));
                
                _isBreaking = false;
            }
        }

        private GameObject _confirmButtonGO;
        IEnumerator AutoConfirmBreak()
        {
            const string path = "UIManager/Popup/Overlay/Panel/Buttons/Confirm Button";
            Button btn = null;

            if (_confirmButtonGO == null)
            {
                while ((_confirmButtonGO = GameObject.Find(path)) == null)
                    yield return null;
            }

            btn = _confirmButtonGO.GetComponent<Button>();

            yield return new WaitUntil(new System.Func<bool>(() => btn != null && btn.isActiveAndEnabled));

            btn.onClick.Invoke();

            yield return null;
        }

        private bool CanShoot() =>
            _bowPurchasedSkill.unlocked
            && (_bowBoostSkill.unlocked || !_boost.IsActive())
            && !_pm.bowDisabled
            && !_windDash.IsActive()
            && _pm.isMoving
            && _rageModeManager.currentState == RageModeManager.RageModeStates.NotActive;

        private bool CanJumpRunner() =>
            GameState.IsRunner()
            && _pm.IsGrounded()
            && _pm.isMoving
            && !_isJumping;

        private void ShortSingleJump()
        {
            _jumpPanel.OnPointerDown(_jumpSpot);
            _jumpPanel.OnPointerUp(_jumpSpot);
            _isJumping = false;
        }

        private IEnumerator LargeSingleJump()
        {
            _jumpPanel.OnPointerDown(_jumpSpot);
            yield return new WaitForSeconds(0.15f);
            _jumpPanel.OnPointerUp(_jumpSpot);
            _isJumping = false;
        }

        private IEnumerator Stage3DelayAndJump()
        {
            // hold off jumping
            yield return new WaitForSeconds(0.75f);

            // now do the normal short jump
            ShortSingleJump();

            // allow the next jump
            _isJumping = false;
        }

        private IEnumerator ShootArrows()
        {
            _pm.ShootArrow();
            yield return new WaitForSeconds(0.1f);
            _isShooting = false;
        }

        private IEnumerator AttackGiant()
        {
            _jumpPanel.OnPointerDown(_jumpSpot);
            _jumpPanel.OnPointerUp(_jumpSpot);
            yield return new WaitForSeconds(0.2f);
            _isAttacking = false;
        }

        [HarmonyPatch(typeof(RandomBox), nameof(RandomBox.OnObjectSpawn))]
        public class Patch_RandomBox_OnObjectSpawn
        {
            [HarmonyPostfix]
            public static void Postfix(RandomBox __instance)
            {
                if (__instance == null) return;

                if (GameState.IsBonus() && AutoJump.Instance.BootsPurchased)
                    AutoJump.Instance.LockBoots();

                if (AutoJump.Instance._bonusSection == 3)
                    AutoJump.Instance._bonusSection3Completed = true;
            }
        }
        private void LockBoots() => _bootsSkill.unlocked = false;

        [HarmonyPatch(typeof(Arrow), nameof(Arrow.Awake))]
        static class Patch_Arrow_Awake
        {
            static void Postfix(Arrow __instance)
            {
                if (AutoJump.OriginalArrowSpeed < 0f)
                {
                    AutoJump.OriginalArrowSpeed = __instance.speed;
                    AutoJump.OriginalElectroSpeed = __instance.electroShotSpeed;

                    AutoJump.NewArrowSpeed = __instance.speed * 1.5f;
                    AutoJump.NewElectroSpeed = __instance.electroShotSpeed * 1.5f;

                    AutoJump.Instance.ChangeArrowSpeed(__instance);
                }
            }
        }

        [HarmonyPatch(typeof(Arrow), nameof(Arrow.OnObjectSpawnOverride))]
        public class Patch_Projectile_OnObjectSpawn
        {
            [HarmonyPostfix]
            public static void Postfix(Arrow __instance)
            {
                if (__instance == null) return;

                if (AutoJump.NewArrowSpeed > 0f)
                    AutoJump.Instance.ChangeArrowSpeed(__instance);
                _activeArrows.Add(__instance);
            }

        }
        private void ChangeArrowSpeed(Arrow arrow)
        {
            if (IsWindDashEnabled() && _windDash.GetCooldown() < 1)
            {
                arrow.speed = AutoJump.NewArrowSpeed;
                arrow.electroShotSpeed = AutoJump.NewElectroSpeed;
            }
            else
            {
                arrow.speed = AutoJump.OriginalArrowSpeed;
                arrow.electroShotSpeed = AutoJump.OriginalElectroSpeed;
            }
        }

        private void HandleMapKeys()
        {
            if (Input.GetKeyDown(KeyCode.F1))
                _mapCtrl.ChangeMap(_mapCtrl.CurrentBonusMap());

            if (Input.GetKeyDown(KeyCode.F2)
                && _bonusStage2Skill != null
                && _bonusGaps2Skill != null)
            {
                MelonCoroutines.Start(SwitchStage2());
            }

            if (Input.GetKeyDown(KeyCode.F3)
                && _bootsSkill != null
                && !_bonusStage3Skill.unlocked
                && !_bonusGaps3Skill.unlocked)
            {
                _bootsSkill.unlocked = !_bootsSkill.unlocked;
                _bootsPurchased = _bootsSkill.unlocked;
            }

            if (Input.GetKeyDown(KeyCode.F4)
                && _bonusStage3Skill != null
                && _bootsSkill.unlocked
                && !_bonusGaps3Skill.unlocked)
                _bonusStage3Skill.unlocked = !_bonusStage3Skill.unlocked;

            if (Input.GetKeyDown(KeyCode.F5)
                && _bonusGaps3Skill != null
                && _bootsSkill.unlocked
                && _bonusStage3Skill.unlocked)
                _bonusGaps3Skill.unlocked = !_bonusGaps3Skill.unlocked;

            if (Input.GetKeyDown(KeyCode.F6))
                _mapCtrl.ChangeMap(_mapCtrl.lastRunnerMap);

            if (Input.GetKeyDown(KeyCode.F7))
                dummy = IsBscLoaded();

            if (Input.GetKeyDown(KeyCode.F8))
            {
                _bonusMapCtrl.spiritBoostEnabled = true;
                _mapCtrl.ChangeMap(_mapCtrl.CurrentBonusMap());
            }


            if (Input.GetKeyDown(KeyCode.F9))
            {
                _bowPurchasedSkill.unlocked = !_bowPurchasedSkill.unlocked;

            }

            if (Input.GetKeyDown(KeyCode.F10))
            {
                _bowBoostSkill.unlocked = !_bowBoostSkill.unlocked;
            }
        }

        IEnumerator SwitchStage2()
        {
            if (_bonusStage2Skill.unlocked && _bonusGaps2Skill.unlocked)
            {
                _bonusGaps2Skill.unlocked = false;
                yield return new WaitForSeconds(0.5f);
                _bonusStage2Skill.unlocked = false;
            }
            else if (!_bonusStage2Skill.unlocked && !_bonusGaps2Skill.unlocked)
            {
                _bonusStage2Skill.unlocked = true;
                yield return new WaitForSeconds(0.5f);
                _bonusGaps2Skill.unlocked = true;
            }
        }
    }
}