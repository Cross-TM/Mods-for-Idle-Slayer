using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IdleSlayerMods.Common;

public sealed class ModHelper : MonoBehaviour
{
    /// <summary>
    /// Event that occurs once the ModHelper has been added to the game scene and is ready to use.
    /// </summary>
    public static event Action<ModHelper>? ModHelperMounted;
    private AchievementManager? _achievementManager;
    private NotificationText? _notificationText;
    private Popup? _popup;
    private Transform? _infoPanelButtonsContainer;
    private GameObject? _templateButton;
    
    private void OnModHelperMounted()
    {
        ModHelperMounted?.Invoke(this);
        Plugin.Log.LogInfo("ModHelper mounted successfully");
    }

    private void Awake()
    {
        OnModHelperMounted();
        _notificationText = GameObject.Find(PathConstants.NotificationTextPath).GetComponent<NotificationText>();
        _popup = GameObject.Find(PathConstants.PopupPath).GetComponent<Popup>();
        _achievementManager = GameObject.Find(PathConstants.AchievementManagerPath).GetComponent<AchievementManager>();
        _infoPanelButtonsContainer = GameObject.Find(PathConstants.ButtonPanelPath).transform;
        _templateButton = ButtonHelper.CreateTemplateButton($"{PathConstants.ButtonPanelPath}/Options");
    }

    /// <summary>
    /// Add an interactable button to the info panel.
    /// This is the same place as Achievements, Options, etc.
    /// </summary>
    /// <param name="text">Text to display on the button, internally also sets the object name.</param>
    /// <param name="clickAction">Action delegate that occurs on button click.</param>
    /// <param name="icon">Icon to display on the button.</param>
    public void AddPanelButton(string text, Action? clickAction = null, Sprite? icon = null)
    {
        Plugin.Log.LogInfo($"Registering panel button: {text}");
        var button = Instantiate(_templateButton, _infoPanelButtonsContainer?.transform);
        if (button is null)
        {
            Plugin.Log.LogWarning("Unable to instantiate template panel button");
            return;
        }
        
        button.name = text;
        button.transform.GetChild(0).GetComponent<Image>().sprite = icon;
        button.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = text;
        button.GetComponent<Button>().onClick.AddListener(clickAction);
        Plugin.Log.LogInfo($"Button {text} added successfully");
    }

    /// <summary>
    /// Show a dialog popup with information and interactable buttons.
    /// </summary>
    /// <param name="title">The top title of the dialog.</param>
    /// <param name="subtitle">The subtitle of the dialog.</param>
    /// <param name="descriptionText">The general message to show on the dialog.</param>
    /// <param name="descriptionColor">Colour of the description text.</param>
    /// <param name="centerDescription">Whether to center the description text in the vertical space.</param>
    /// <param name="confirmText">Confirm button text. If left empty, this button won't render.</param>
    /// <param name="confirmAction">Action to execute on confirm button press.</param>
    /// <param name="cancelText">Cancel button text.</param>
    /// <param name="cancelAction">Action to execute on confirm button press. Closes dialog by default.</param>
    /// <param name="image">The sprite image to show near the title. If left empty, this image won't render.</param>
    /// <param name="overridePopup">If true, any existing popup will be overridden by this popup. If false, this will display on top of any existing popups.</param>
    public void ShowDialog(string title = "Dialog", string subtitle = "", string descriptionText = "", Color descriptionColor = default, bool centerDescription = false, string confirmText = "", Action? confirmAction = null, string cancelText = "Close", Action? cancelAction = null, Sprite? image = null,bool overridePopup = false)
    {
        _popup?.Show(new()
        {
            title = title,
            subtitle = subtitle,
            confirmActionText = confirmText,
            confirmAction = confirmAction,
            overrideCurrentPopup = overridePopup,
            description = descriptionText,
            descriptionColor = descriptionColor,
            cancelActionText = cancelText,
            cancelAction = cancelAction,
            centeredDescription = centerDescription,
            sprite = image
        });
    }

    /// <summary>
    /// Display an achievement unlocked popup
    /// </summary>
    /// <param name="achievement">The achievement to display.
    /// See <see cref="Achievement"/> for more information, and use the <see cref="AchievementHelper"/> class to easily create an achievement.
    /// <seealso cref="AchievementHelper.CreateAchievement"/>
    /// </param>
    public void ShowAchievement(Achievement achievement) => _achievementManager?.ShowNextUnlockedAchivement(achievement);

    /// <summary>
    /// Show a text in the center of the screen, the same type as lucky box event texts.
    /// </summary>
    /// <param name="message">The message to display.</param>
    /// <param name="shine">Whether to play a sparkle/shine animation with this text.</param>
    public void ShowNotification(string message, bool shine)
    {
        Plugin.Log.LogDebug($"Attempting to show notification with message: {message}");
        _notificationText?.Show(message, shine);
    }
}