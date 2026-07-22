using ArmoryManager.Core;
using Il2Cpp;
using UnityEngine;
using UnityEngine.UI;

namespace ArmoryManager.Game;

internal sealed class ArmoryVisuals
{
    private static readonly Color ProtectedColor = new(1f, 0.72f, 0.12f, 1f);
    private static readonly Color ReservedColor = new(1f, 0.55f, 0.1f, 1f);

    private readonly WeaponsManager _weaponsManager;
    private readonly ProtectedItemStore _protectedItems;
    private readonly Dictionary<int, OutlineState> _outlines = new();
    private readonly Dictionary<int, ImageState> _slotImages = new();
    private readonly Dictionary<int, Text> _favoriteStars = new();

    internal ArmoryVisuals(WeaponsManager weaponsManager, ProtectedItemStore protectedItems)
    {
        _weaponsManager = weaponsManager;
        _protectedItems = protectedItems;
    }

    internal void Refresh(int desiredFreeSlots)
    {
        RestoreSlotColors();
        DisableProtectionOutlines();
        DisableFavoriteStars();

        var slots = _weaponsManager.allSlots;
        if (slots == null)
            return;

        var reservedStart = Math.Max(0, slots.Length - desiredFreeSlots);
        for (var index = 0; index < slots.Length; index++)
        {
            var slot = slots[index];
            if (slot == null)
                continue;

            if (index >= reservedStart)
                TintReservedSlot(slot);

            var itemObject = slot.GetComponentInChildren<ItemObject>();
            if (itemObject?.currentItem == null)
                continue;

            if (_protectedItems.Contains(itemObject.currentItem.uid))
            {
                EnableOutline(itemObject.icon, ProtectedColor);
                EnableFavoriteStar(itemObject.icon);
            }
        }
    }

    internal void Restore()
    {
        RestoreSlotColors();
        foreach (var state in _outlines.Values)
        {
            if (state.Outline == null)
                continue;

            if (state.Created)
                UnityEngine.Object.Destroy(state.Outline);
            else
                state.Restore();
        }

        _outlines.Clear();

        foreach (var star in _favoriteStars.Values)
        {
            if (star != null)
                UnityEngine.Object.Destroy(star.gameObject);
        }

        _favoriteStars.Clear();
    }

    private void TintReservedSlot(WeaponItemSlot slot)
    {
        var image = slot.GetComponent<Image>();
        if (image == null)
            return;

        var id = image.GetInstanceID();
        if (!_slotImages.ContainsKey(id))
            _slotImages[id] = new ImageState(image, image.color);

        image.color = Color.Lerp(_slotImages[id].OriginalColor, ReservedColor, 0.35f);
    }

    private void RestoreSlotColors()
    {
        foreach (var state in _slotImages.Values)
        {
            if (state.Image != null)
                state.Image.color = state.OriginalColor;
        }
    }

    private void DisableProtectionOutlines()
    {
        foreach (var state in _outlines.Values)
        {
            if (state.Outline == null)
                continue;

            if (state.Created)
                state.Outline.enabled = false;
            else
                state.Restore();
        }
    }

    private void EnableOutline(Image icon, Color color)
    {
        if (icon == null)
            return;

        var id = icon.GetInstanceID();
        if (!_outlines.TryGetValue(id, out var state) || state.Outline == null)
        {
            var outline = icon.GetComponent<Outline>();
            var created = outline == null;
            outline ??= icon.gameObject.AddComponent<Outline>();
            state = new OutlineState(outline, created);
            _outlines[id] = state;
        }

        state.Outline.enabled = true;
        state.Outline.effectColor = color;
        state.Outline.effectDistance = new Vector2(2f, -2f);
        state.Outline.useGraphicAlpha = true;
    }

    private void DisableFavoriteStars()
    {
        foreach (var star in _favoriteStars.Values)
        {
            if (star != null)
                star.gameObject.SetActive(false);
        }
    }

    private void EnableFavoriteStar(Image icon)
    {
        if (icon == null)
            return;

        var id = icon.GetInstanceID();
        if (!_favoriteStars.TryGetValue(id, out var star) || star == null)
        {
            var starObject = new GameObject("ArmoryManager Favorite Star");
            starObject.transform.SetParent(icon.transform, false);
            star = starObject.AddComponent<Text>();
            star.text = "★";
            star.color = ProtectedColor;
            star.alignment = TextAnchor.UpperRight;
            star.fontSize = 20;
            star.resizeTextForBestFit = true;
            star.resizeTextMinSize = 10;
            star.resizeTextMaxSize = 24;
            star.raycastTarget = false;
            star.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            var rect = star.rectTransform;
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-1f, -1f);
            rect.sizeDelta = new Vector2(24f, 24f);

            var shadow = starObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            shadow.effectDistance = new Vector2(1f, -1f);
            shadow.useGraphicAlpha = true;
            _favoriteStars[id] = star;
        }

        star.gameObject.SetActive(true);
    }

    private sealed class ImageState
    {
        internal ImageState(Image image, Color originalColor)
        {
            Image = image;
            OriginalColor = originalColor;
        }

        internal Image Image { get; }
        internal Color OriginalColor { get; }
    }

    private sealed class OutlineState
    {
        internal OutlineState(Outline outline, bool created)
        {
            Outline = outline;
            Created = created;
            OriginalEnabled = outline.enabled;
            OriginalColor = outline.effectColor;
            OriginalDistance = outline.effectDistance;
            OriginalUseGraphicAlpha = outline.useGraphicAlpha;
        }

        internal Outline Outline { get; }
        internal bool Created { get; }
        private bool OriginalEnabled { get; }
        private Color OriginalColor { get; }
        private Vector2 OriginalDistance { get; }
        private bool OriginalUseGraphicAlpha { get; }

        internal void Restore()
        {
            Outline.enabled = OriginalEnabled;
            Outline.effectColor = OriginalColor;
            Outline.effectDistance = OriginalDistance;
            Outline.useGraphicAlpha = OriginalUseGraphicAlpha;
        }
    }
}
