using UnityEngine;
using Il2Cpp;
using UnityEngine.UI;
using MelonLoader;
using System.Collections;

namespace ArmoryManager;

public class BreakWeapons : MonoBehaviour
{
    public static BreakWeapons Instance { get; private set; }

    WeaponsManager _weaponsManager;
    PlayerInventory _playerInventory;
    DropsManager _dropsManager;
    int _keep;
    bool _isBreaking;

    public void Awake()
    {
        _weaponsManager = WeaponsManager.instance;
        _playerInventory = PlayerInventory.instance;
        _dropsManager = DropsManager.instance;
        _keep = Plugin.Config.CurrentNumberOfWeapons.Value;

    }

    public void Start()
    {

    }

    public void LateUpdate()
    {
        double before = _keep;
        
        if (Input.GetKeyUp(KeyCode.F7))
        {
            if (_keep + 5 > (int)_weaponsManager.GetMaxSlots())
                _keep = (int)_weaponsManager.GetMaxSlots();
            else
                _keep += 5;

            if (_keep != before)
                Plugin.Logger.Msg($"Weapons to keep: {_keep}");
        }

        if (Input.GetKeyUp(KeyCode.F8))
        {
            if (_keep - 5 < Plugin.Config.LowerBoundOfWeapons.Value)
                _keep = Plugin.Config.LowerBoundOfWeapons.Value;
            else
                _keep -= 5;

            if (_keep != before)
                Plugin.Logger.Msg($"Weapons to keep: {_keep}");
        }

        if (before != _keep)
        {
            Plugin.Config.CurrentNumberOfWeapons.Value = _keep;
        }

        if (!_isBreaking && _weaponsManager.currentItems.Count > _keep && GameState.IsRunner())
        {
            MelonCoroutines.Start(BreakAllExcessWeapons());
        }
    }

    IEnumerator BreakAllExcessWeapons()
    {
        _isBreaking = true;
        var list = _weaponsManager.currentItems;

        // while there are more than we want…
        while (list.Count > _keep)
        {
            int before = list.Count;
            var toBreak = list[list.Count - 1];

            // 2) show the break‑popup
            _weaponsManager.BreakPopup(toBreak);

            // 3) click “Confirm” when it comes up
            yield return AutoConfirmBreak();

            // 4) wait until the item has actually left the list
            yield return new WaitUntil(new System.Func<bool>(() => list.Count < before));
        }

        _isBreaking = false;
    }
    
    private GameObject _confirmButtonGO;
    IEnumerator AutoConfirmBreak()
    {
        const string path = "UIManager/Popup/Overlay/Panel/Buttons/Confirm Button";
        Button btn = null;

        // wait for the confirm button to be in the scene
        if (_confirmButtonGO == null) {
            while ((_confirmButtonGO = GameObject.Find(path)) == null)
                yield return null;
        }

        btn = _confirmButtonGO.GetComponent<Button>();

        // wait until it’s active & enabled
        yield return new WaitUntil(new System.Func<bool>(() => btn != null && btn.isActiveAndEnabled));

        // fire it
        btn.onClick.Invoke();

        // give the UI a frame to start closing
        yield return null;
    }
}