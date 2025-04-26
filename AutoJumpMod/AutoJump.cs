using Il2Cpp;
using MelonLoader;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using HarmonyLib;
using Il2CppSystem;
using System.Reflection;
using Il2CppInterop.Runtime.Injection;
using System;
using System.Linq;

namespace AutoJumpMod;

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

    public static AutoJump Instance { get; private set; }
    public bool BootsPurchased => _bootsPurchased;
    private bool _prevBootsUnlocked;
    private bool _bootsUnlockHandled;

    private bool _autoJump;
    private bool _isJumping;
    private bool _isShooting;
    private bool _didStage3Delay;
    private bool _isAttacking;

    private int _bonusSection;
    private bool _wasClockVisibleLastFrame;
    private bool _bootsPurchased;
    private bool _bootsChanged;

    private System.Type _bscType;
    private FieldInfo _skipField;
    private bool _checked;

    bool dummy;

    private Boost _boost;
    private JumpPanel _jumpPanel;
    private PlayerMovement _pm;
    private PointerEventData _jumpSpot;
    private RageModeManager _rageModeManager;
    private WindDash _windDash;
    private MapController _mapCtrl;
    private BonusMapController _bonusMapCtrl;
    private PlayerInventory _playerInventory;

    private AscensionSkill _bootsSkill;
    private AscensionSkill _bonusStage2Skill;
    private AscensionSkill _bonusGaps2Skill;
    private AscensionSkill _bonusStage3Skill;
    private AscensionSkill _bonusGaps3Skill;
    private AscensionSkill _bowPurchasedSkill;
    private AscensionSkill _bowBoostSkill;

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
        _windDash = FindObjectOfType<WindDash>();
        _boost = FindObjectOfType<Boost>();

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
    }

    void LateUpdate()
    {
        ResetStateOnRunner();
        DetectClockFlips();
        HandleBootsLogic();
        HandleAutoJump();

#if DEBUG
        HandleMapKeys();
#endif
    }
    private void EnsureBscLoaded()
    {
        if (_checked) return;
        _checked = true;

        const string qualName =
            "BonusStageCompleter.BonusStageCompleter, BonusStageCompleter";

        // 1) Grab the managed Type
        _bscType = System.Type.GetType(qualName);
        if (_bscType == null)
        {
            Plugin.DLog($"BSC type not found: {qualName}");
            return;
        }
        Plugin.DLog($"Loaded managed type: {_bscType.FullName}");

        // 2) Cache the private field info
        _skipField = _bscType.GetField(
            "_skipAtSpiritBoostEnabled",
            BindingFlags.NonPublic | BindingFlags.Instance
        );
        if (_skipField == null)
            Plugin.DLog("Could not find _skipAtSpiritBoostEnabled on BSC");

    }

    private UnityEngine.Object FindBscInstance()
    {
        EnsureBscLoaded();
        if (_bscType == null) return null;

        // --- reflection magic to call: T UnityEngine.Object.FindObjectOfType<T>() ---
        var findMethod = typeof(UnityEngine.Object)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(m =>
                m.Name == "FindObjectOfType"
                && m.IsGenericMethodDefinition
                && m.GetGenericArguments().Length == 1
                && m.GetParameters().Length == 0
            );
        if (findMethod == null)
        {
            Plugin.Logger.Error("Couldn't find generic FindObjectOfType<>()");
            return null;
        }

        // make it FindObjectOfType<BonusStageCompleter>()
        var generic = findMethod.MakeGenericMethod(_bscType);
        var instance = generic.Invoke(null, System.Array.Empty<object>())
                       as UnityEngine.Object;
        return instance;
    }
    public bool IsBscLoaded()
    {
        return FindBscInstance() != null;
    }

    public bool ShouldSkipAtSpiritBoost()
    {
        var inst = FindBscInstance();
        if (inst == null || _skipField == null) return false;

        // pull out the private bool
        return (bool)_skipField.GetValue(inst);
    }

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
    }

    private void DetectClockFlips()
    {
        bool showTime = _bonusMapCtrl.showCurrentTime;
        
        bool inStage1 = GameState.IsBonus()
                        && _mapCtrl.CurrentBonusMap() == Maps.list.BonusStage;
        bool inStage3 = GameState.IsBonus()
                        && _mapCtrl.CurrentBonusMap() == Maps.list.BonusStage3;
        
        if ((inStage1 || inStage3) && !_wasClockVisibleLastFrame && showTime)
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
            if (_bonusSection == 3)
            {
                _bootsSkill.unlocked = _pm.isMoving;
            }
            else
            {
                if (_bonusMapCtrl.showCurrentTime)
                    _bootsSkill.unlocked = true;
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
        yield return new WaitForSeconds(0.5f);

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
                AutoJump.Instance._bonusSection = 4;
        }
    }
    private void LockBoots() => _bootsSkill.unlocked = false;
}
