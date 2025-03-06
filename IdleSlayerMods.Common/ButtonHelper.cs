using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace IdleSlayerMods.Common;

public static class ButtonHelper
{
    /// <summary>
    /// Duplicate an info panel button, and set values to default.
    /// </summary>
    /// <param name="templatePath">Path of the button GameObject.</param>
    /// <returns>New GameObject instance of an info panel button.</returns>
    internal static GameObject CreateTemplateButton(string templatePath)
    {
        var button = Object.Instantiate(GameObject.Find(templatePath));
        button.transform.GetChild(0).GetComponent<Image>().sprite = null;
        button.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = "Button";
        button.GetComponent<Button>().onClick = null;
        return button;
    }
}