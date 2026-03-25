using Il2Cpp;
using MelonLoader;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using HarmonyLib;
using UnityEngine.UI;
using System.Text;


namespace AutoJumpMod
{
    // Helper to query other mods via reflection

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
        private static readonly List<Arrow> _activeArrows = [];


        public static AutoJump Instance { get; private set; }
        public bool BootsPurchased => _bootsPurchased;

        private bool _prevBootsUnlocked;
        private bool _bootsUnlockHandled;
        private bool _autoJump;
        private bool _isJumping;
        private bool _isShooting;
        private bool _didStage1Delay;
        private bool _didStage2Delay;
        private bool _isAttacking;
        private bool _isBreaking;
        private bool _wasClockVisibleLastFrame;
        private bool _bootsPurchased;
        private bool _bootsChanged;
        private bool dummy;
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
/*        private readonly ModFlagChecker _armoryManagerChecker = new(
            "ArmoryManager.BreakWeapons, ArmoryManager"
        );
*/
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
            if (Instance && Instance != this)
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

            CustomAwake();
            
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
                foreach (var RandomEvent in REM)
                {
                    if (RandomEvent.name != "Dual Randomness") continue;
                    dual = RandomEvent;
                    dualChance = RandomEvent.chance;
                    break;
                }
            }

            CustomStart();
        }


        private float _lastCleanupTime;
        private float _lastModCheckTime;
        private bool _isBscLoadedCached;
        private bool _isAutoBoostLoadedCached;
//        private bool _isArmoryManagerLoadedCached;

        void LateUpdate()
        {
            var now = Time.time;
            if (now - _lastCleanupTime > 0.5f)
            {
                _activeArrows.RemoveAll(a => !a || a.outOfCamera);
                _lastCleanupTime = now;
            }

            if (now - _lastModCheckTime > 1f)
            {
                _isBscLoadedCached = _bscChecker.IsLoaded();
                _isAutoBoostLoadedCached = _autoBoostChecker.IsLoaded();
/*                _isArmoryManagerLoadedCached = _armoryManagerChecker.IsLoaded();
*/                _lastModCheckTime = now;
            }

            JumpToggle();
            HandleArrowSpeedOnWindDash();
            ResetStateOnRunner();
            DetectClockFlips();
            HandleBootsLogic();
            HandleAutoJump();
//            MiniArmoryManager();
#if DEBUG
        HandleMapKeys();
#endif
            CustomBuild();
        }

        // speeds up all on-screen arrows when windDash just activated
        private void HandleArrowSpeedOnWindDash()
        {
            var windActive = IsWindDashEnabled() && _windDash.GetCooldown() < 1;
            if (windActive && !_prevWindDashActive)
            {
                foreach (var arrow in _activeArrows)
                    ChangeArrowSpeed(arrow);
            }
            _prevWindDashActive = windActive;
        }


        public bool IsBscLoaded() => _isBscLoadedCached;
        public bool ShouldSkipAtSpiritBoost() => _bscChecker.GetBoolFlag();
        public bool IsAutoBoostLoaded() => _isAutoBoostLoadedCached;
        public bool IsWindDashEnabled() => _autoBoostChecker.GetBoolFlag();
//        public bool IsArmoryManagerLoaded() => _isArmoryManagerLoadedCached;


        void OnDestroy() => Instance = Instance == this ? null : Instance;

        private void InitializeSkills()
        {
            foreach (var skill in _playerInventory.ascensionSkills)
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
            var showTime = _bonusMapCtrl.showCurrentTime;

            var currentMap = _mapCtrl.CurrentBonusMap();
            var inStage1 = GameState.IsBonus()
                           && currentMap.id == Maps.list.BonusStage.id;
            var inStage3 = GameState.IsBonus()
                           && currentMap.id == Maps.list.BonusStage3.id;

            if ((inStage1 || inStage3) && !_wasClockVisibleLastFrame && showTime && _bonusMapCtrl.currentSectionIndex == _bonusSection)
                _bonusSection++;
            _wasClockVisibleLastFrame = showTime;
        }

        private void HandleBootsLogic()
        {
            if (!_bootsUnlockHandled)
            {
                var nowUnlocked = _bootsSkill.unlocked;
                if (!_prevBootsUnlocked && nowUnlocked)
                {
                    _bootsUnlockHandled = true;
                    _bootsPurchased = true;
                }
                _prevBootsUnlocked = nowUnlocked;
            }

            var inStage3 = GameState.IsBonus()
                           && _mapCtrl.CurrentBonusMap() == Maps.list.BonusStage3;
            var inStage2 = GameState.IsBonus()
                           && _mapCtrl.CurrentBonusMap() == Maps.list.BonusStage2;

            if (!_autoJump || !AutoBonusLevel()) return;
            
            if (inStage3 && _bootsPurchased)
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

                if (dual)
                {
                    dual.chance = _bonusSection == 2 ? 0f : dualChance;
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

        private void JumpToggle()
        {
            if (!Input.GetKeyDown(Plugin.Config.AutoJumpToggleKey.Value)) return;
            
            _autoJump = !_autoJump;
            Plugin.Logger.Msg($"AutoJump is: {(_autoJump ? "ON" : "OFF")} ");
            Plugin.ModHelperInstance.ShowNotification(
                _autoJump ? "Auto Jump enabled!" : "Auto Jump disabled!",
                _autoJump
            );
            Plugin.Config.UseAutoJump.Value = _autoJump;
        }

        private void HandleAutoJump()
        {

            if (!_autoJump) return;

            var currentMap = _mapCtrl.CurrentBonusMap();
            var inStage1 = GameState.IsBonus()
                           && currentMap.id == Maps.list.BonusStage.id;
            var inStage3 = GameState.IsBonus()
                           && currentMap.id == Maps.list.BonusStage3.id;

            if (!inStage3 || _bonusSection != 2)
                _didStage2Delay = false;
            if (!inStage3 || _bonusSection != 1)
                _didStage1Delay = false;

            if (GameState.IsBonus()
                && !_isJumping
                && AutoBonusLevel())
            { 

                _isJumping = true;
                if (inStage1 && _pm.IsGrounded() && _bonusSection == 1 && _bonusMapCtrl.showCurrentTime)
                {
                    MelonCoroutines.Start(LargeSingleJump());
                }
                else if (inStage3 && _bonusSection == 1 && !_didStage1Delay)
                {
                    _didStage1Delay = true;
                    MelonCoroutines.Start(Stage1Or2DelayAndJump());
                }
                else if (inStage3 && _bonusSection == 2 && !_didStage2Delay)
                {
                    _didStage2Delay = true;
                    MelonCoroutines.Start(Stage1Or2DelayAndJump());
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
            if (!GameState.IsRunner() || _pm.isMoving || _isAttacking) return;
            _isAttacking = true;
            MelonCoroutines.Start(AttackGiant());
        }

/*        private void MiniArmoryManager()
        {
            if (IsArmoryManagerLoaded()) return;
            
            if (!_isBreaking && !_weaponsManager.hasFreeSlot && GameState.IsRunner())
            {
                MelonCoroutines.Start(BreakLastWeapon());
            }
        }

        private IEnumerator BreakLastWeapon() {
            if (!_weaponsManager) yield return null;

            if (_weaponsManager.hasFreeSlot) yield break;
            
            _isBreaking = true;
            var list = _weaponsManager.currentItems;

            var before = list.Count;
            var toBreak = list[^1];

            // 2) show the break‑popup
            _weaponsManager.BreakPopup(toBreak);

            // 3) click “Confirm” when it comes up
            yield return AutoConfirmBreak();

            // 4) wait until the item has actually left the list
            yield return new WaitUntil(new Func<bool>(() => list.Count < before));

            _isBreaking = false;
        }

        private GameObject _confirmButtonGO;

        private IEnumerator AutoConfirmBreak()
        {
            const string path = "UIManager/Popup/Overlay/Panel/Buttons/Confirm Button";

            if (!_confirmButtonGO)
            {
                while (!(_confirmButtonGO = GameObject.Find(path)))
                    yield return null;
            }

            var btn = _confirmButtonGO.GetComponent<Button>();

            yield return new WaitUntil(new Func<bool>(() => btn && btn.isActiveAndEnabled));

            btn.onClick.Invoke();

            yield return null;
        }*/

        private bool CanShoot() =>
            _bowPurchasedSkill.unlocked
            && (_bowBoostSkill.unlocked || !_boost.IsActive())
            && !_pm.bowDisabled
            && !_windDash.IsActive()
            && _pm.isMoving
            && _rageModeManager.currentState == RageModeManager.RageModeStates.NotActive
            && !IsShootingDisabled();

        private bool AutoBonusLevel() =>
            IsBscLoaded()
            && (ShouldSkipAtSpiritBoost() || !_bonusMapCtrl.spiritBoostEnabled);

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

        private IEnumerator Stage1Or2DelayAndJump()
        {

            yield return new WaitForSeconds(0.5f);
            // hold off jumping
            while (_pm.IsGrounded())
            {
                yield return null;
            }

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
            _isAttacking = false;
            yield return null;
        }

        [HarmonyPatch(typeof(RandomBox), nameof(RandomBox.OnObjectSpawn))]
        public class Patch_RandomBox_OnObjectSpawn
        {
            [HarmonyPostfix]
            public static void Postfix(RandomBox __instance)
            {
                if (__instance == null) return;

                if (GameState.IsBonus() && Instance.BootsPurchased)
                    Instance.LockBoots();

                if (Instance._bonusSection == 3)
                    Instance._bonusSection3Completed = true;
            }
        }
        private void LockBoots() => _bootsSkill.unlocked = false;

        [HarmonyPatch(typeof(Arrow), nameof(Arrow.Awake))]
        static class Patch_Arrow_Awake
        {
            static void Postfix(Arrow __instance)
            {
                if (OriginalArrowSpeed < 0f)
                {
                    OriginalArrowSpeed = __instance.speed;
                    OriginalElectroSpeed = __instance.electroShotSpeed;

                    NewArrowSpeed = __instance.speed * 1.5f;
                    NewElectroSpeed = __instance.electroShotSpeed * 1.5f;

                    Instance.ChangeArrowSpeed(__instance);
                }
            }
        }

        [HarmonyPatch(typeof(Arrow), nameof(Arrow.OnObjectSpawnOverride))]
        public class Patch_Projectile_OnObjectSpawn
        {
            [HarmonyPostfix]
            public static void Postfix(Arrow __instance)
            {
                if (!__instance) return;

                if (NewArrowSpeed > 0f)
                    Instance.ChangeArrowSpeed(__instance);
                _activeArrows.Add(__instance);
            }

        }
        
        private void ChangeArrowSpeed(Arrow arrow)
        {
            if (IsWindDashEnabled() && _windDash.GetCooldown() < 1)
            {
                arrow.speed = NewArrowSpeed;
                arrow.electroShotSpeed = NewElectroSpeed;
            }
            else
            {
                arrow.speed = OriginalArrowSpeed;
                arrow.electroShotSpeed = OriginalElectroSpeed;
            }
        }

        private void HandleMapKeys()
        {
            if (Input.GetKeyDown(KeyCode.F1))
                _mapCtrl.ChangeMap(_mapCtrl.CurrentBonusMap());

            if (Input.GetKeyDown(KeyCode.F2)
                && _bonusStage2Skill
                && _bonusGaps2Skill)
            {
                MelonCoroutines.Start(SwitchStage2());
            }

            if (Input.GetKeyDown(KeyCode.F3)
                && _bootsSkill
                && !_bonusStage3Skill.unlocked
                && !_bonusGaps3Skill.unlocked)
            {
                _bootsSkill.unlocked = !_bootsSkill.unlocked;
                _bootsPurchased = _bootsSkill.unlocked;
            }

            if (Input.GetKeyDown(KeyCode.F4)
                && _bonusStage3Skill
                && _bootsSkill.unlocked
                && !_bonusGaps3Skill.unlocked)
                _bonusStage3Skill.unlocked = !_bonusStage3Skill.unlocked;

            if (Input.GetKeyDown(KeyCode.F5)
                && _bonusGaps3Skill
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

        private IEnumerator SwitchStage2()
        {
            switch (_bonusStage2Skill.unlocked)
            {
                case true when _bonusGaps2Skill.unlocked:
                    _bonusGaps2Skill.unlocked = false;
                    yield return new WaitForSeconds(0.5f);
                    _bonusStage2Skill.unlocked = false;
                    break;
                case false when !_bonusGaps2Skill.unlocked:
                    _bonusStage2Skill.unlocked = true;
                    yield return new WaitForSeconds(0.5f);
                    _bonusGaps2Skill.unlocked = true;
                    break;
            }
        }

        /// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // For My Own Build Only    

        private static string EnvVarName => Encoding.UTF8.GetString(Convert.FromBase64String("SURMRVNMQVlFUl9DVVNUT01CVUlMRA=="));

        public static bool IsCustomBuild => Environment.GetEnvironmentVariable(EnvVarName) == "1";
        private bool IsShootingDisabled() => _shootingDisabled & IsCustomBuild;

        private bool _hordeEventPause;
        private bool _prevWindDashActive;
        private bool _shootingDisabled;

        private double _playerDecreaseRandomBoxCoinsChances;

        public int Threshold = 30;
        public float TriggerSeconds = 20f;
        public float ResetTimer = 20f;

        private int _count;
        private float _windowStart = -1;
        private bool _DualPaused;

        private void CustomAwake()
        {
            if (IsCustomBuild)
            {
                _shootingDisabled = false;
                _hordeEventPause = false;
            }
        }

        private void CustomStart()
        { 
            if (IsCustomBuild)
                _playerDecreaseRandomBoxCoinsChances = _playerInventory.decreaseRandomBoxCoinsChances;            
        }

        private void CustomBuild()
        {
            if (!IsCustomBuild) return;
            
            HandleShootingToggle();

            //If attacking something with the sword
            if (!GameState.IsRunner() || _pm.isMoving || _isAttacking || !_hordeEventPause) return;
            
            _isAttacking = true;
            MelonCoroutines.Start(AttackGiant());
        }

        private void HandleShootingToggle()
        {
            if (!Input.GetKeyDown(KeyCode.X)) return;
            
            _shootingDisabled = !_shootingDisabled;
            Plugin.Logger.Msg($"Shooting is: {(_shootingDisabled ? "OFF" : "ON")} ");
            Plugin.ModHelperInstance.ShowNotification(
                _shootingDisabled ? "Shooting disabled!" : "Shooting enabled!",
                _shootingDisabled
            );

            if (_shootingDisabled || !_hordeEventPause) return;
            
            _autoJump = true;
            _hordeEventPause = false;
        }

        public void RegisterDualEvent()
        {
            var now = Time.realtimeSinceStartup;

            if (_DualPaused) return;
            
            // start or reset the window
            if (_windowStart < 0 || now - _windowStart > TriggerSeconds)
            {
                _windowStart = now;
                _count = 0;
            }

            // count it
            _count++;

            // if we’ve hit the threshold, fire the first callback + start the 2nd-delay
            if (_count < Threshold) return;
            
            _DualPaused = true;
            Plugin.Logger.Msg("Dual Randomness paused");
            MelonCoroutines.Start(PostThresholdDelay());
        }

        private IEnumerator PostThresholdDelay()
        {
            yield return new WaitForSeconds(ResetTimer);
            // reset ready for the next cycle
            _DualPaused = false;
            _windowStart = -1;
            _count = 0;
            Plugin.Logger.Msg("Dual Randomness restarted");
        }

        private const string DualRandomnessName = "Dual Randomness";

        [HarmonyPatch(typeof(RandomEvent), nameof(RandomEvent.Activate))]
        public class Patch_RandomEvent_Activate
        {
            [HarmonyPostfix]
            public static void Postfix(RandomEvent __instance)
            {
                if (__instance == null || !IsCustomBuild) return;
                if (__instance.name == DualRandomnessName)
                {
                    Instance?.RegisterDualEvent();
                }
            }
        }

        [HarmonyPatch(typeof(Horde), "OnEventStart")]
        public class HordeEventStartPatch
        {

            [HarmonyPostfix]
            public static void OnHordeEventStart()
            {
                if (!IsCustomBuild) return;
                if (Instance._autoJump && Instance._shootingDisabled)
                {
                    Instance._autoJump = false;
                    Instance._hordeEventPause = true;
                }
            }
        }

        [HarmonyPatch(typeof(Horde), "OnEventEnd")]
        public class HordeEventEndPatch
        {

            [HarmonyPostfix]
            public static void OnHordeEventEnd()
            {
                if (!IsCustomBuild) return;
                if (!Instance._hordeEventPause) return;
                
                Instance._autoJump = true;
                Instance._hordeEventPause = false;
            }
        }
    }
}