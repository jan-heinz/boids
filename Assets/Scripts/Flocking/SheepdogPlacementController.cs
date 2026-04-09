using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// handles selecting and moving the available sheepdogs in the scene
public class SheepdogPlacementController : MonoBehaviour
{
    // -------------------------------------------------------------------------------------------------------------

    [SerializeField]
    [Tooltip("Legacy fallback sheepdog reference when only one dog is configured")]
    private Sheepdog sheepdog;

    [SerializeField]
    [Tooltip("Legacy fallback label kept for old scene data")]
    private string cardLabel = "1";

    [SerializeField]
    [Tooltip("Optional default sprite shown on sheepdog cards")]
    private Sprite cardSprite;

    [SerializeField, Min(1f)]
    [Tooltip("Width of each sheepdog card")]
    private float cardWidth = 88f;

    [SerializeField, Min(1f)]
    [Tooltip("Height of each sheepdog card")]
    private float cardHeight = 96f;

    [SerializeField, Min(0f)]
    [Tooltip("Padding above the card row from the bottom of the screen")]
    private float cardBottomMargin = 24f;

    [SerializeField, Min(0f)]
    [Tooltip("Spacing between sheepdog cards")]
    private float cardSpacing = 12f;

    [SerializeField, Min(1f)]
    [Tooltip("Width of the dog sprite inside each card")]
    private float cardSpriteWidth = 56f;

    [SerializeField, Min(1f)]
    [Tooltip("Height of the dog sprite inside each card")]
    private float cardSpriteHeight = 48f;

    private readonly List<Sheepdog> sheepdogs = new();
    private readonly List<Sprite> cardSprites = new();
    private int selectedIndex = -1;
    private Texture2D cardFillTexture;
    private Texture2D selectedCardFillTexture;
    private Texture2D cardOutlineTexture;
    private Texture2D selectedCardOutlineTexture;
    private GUIStyle cardLabelStyle;
    private GUIStyle cardNameStyle;

    // finds the sheepdogs when needed and builds the card styles
    private void Awake()
    {
        cardFillTexture = CreateSolidTexture(new Color(0.15f, 0.14f, 0.12f, 0.95f));
        selectedCardFillTexture = CreateSolidTexture(new Color(0.62f, 0.43f, 0.14f, 0.95f));
        cardOutlineTexture = CreateSolidTexture(new Color(0.5f, 0.43f, 0.3f, 1f));
        selectedCardOutlineTexture = CreateSolidTexture(new Color(0.95f, 0.82f, 0.45f, 1f));
        cardLabelStyle = CreateLabelStyle();
        cardNameStyle = CreateNameStyle();
        RefreshSheepdogs();
    }

    // keeps the card list in sync if sheepdogs are added or removed
    private void OnEnable()
    {
        RefreshSheepdogs();
    }

    // listens for number key selection and click movement orders
    private void Update()
    {
        RefreshSheepdogs();
        HandleHotkeySelection();

        if (selectedIndex < 0 || selectedIndex >= sheepdogs.Count)
        {
            selectedIndex = -1;
            return;
        }

        if (DidCancelSelection())
        {
            selectedIndex = -1;
            return;
        }

        if (!DidConfirmPlacement())
        {
            return;
        }

        if (IsPointerOverAnyCard())
        {
            return;
        }

        Camera cameraToUse = Camera.main;
        Sheepdog selectedSheepdog = sheepdogs[selectedIndex];

        if (cameraToUse == null || selectedSheepdog == null)
        {
            return;
        }

        Vector3 mousePosition = GetMouseScreenPosition();
        Vector3 worldPosition = cameraToUse.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, -cameraToUse.transform.position.z));
        selectedSheepdog.MoveTo(worldPosition);
    }

    // draws one card per sheepdog along the bottom of the screen
    private void OnGUI()
    {
        RefreshSheepdogs();

        for (int i = 0; i < sheepdogs.Count; i++)
        {
            Sheepdog currentSheepdog = sheepdogs[i];

            if (currentSheepdog == null)
            {
                continue;
            }

            Rect cardRect = GetCardRect(i, sheepdogs.Count);
            bool isSelected = i == selectedIndex;

            if (GUI.Button(cardRect, GUIContent.none, GUIStyle.none))
            {
                ToggleSelection(i);
            }

            DrawCardBackground(cardRect, isSelected);

            Rect labelRect = new(cardRect.x, cardRect.y + 8f, cardRect.width, 28f);
            string labelText = sheepdogs.Count == 1 && !string.IsNullOrWhiteSpace(cardLabel)
                ? cardLabel
                : (i + 1).ToString();
            GUI.Label(labelRect, labelText, cardLabelStyle);

            Rect nameRect = new(cardRect.x + 6f, cardRect.y + 34f, cardRect.width - 12f, 18f);
            GUI.Label(nameRect, currentSheepdog.DisplayName, cardNameStyle);

            Sprite sprite = GetCardSprite(i);

            if (sprite == null)
            {
                continue;
            }

            Rect spriteRect = new(
                cardRect.x + ((cardRect.width - cardSpriteWidth) * 0.5f),
                cardRect.y + 52f,
                cardSpriteWidth,
                cardSpriteHeight
            );
            DrawSprite(spriteRect, sprite);
        }
    }

    // rebuilds the sheepdog list using hierarchy order for stable numbering
    private void RefreshSheepdogs()
    {
        Sheepdog[] discoveredSheepdogs = FindObjectsByType<Sheepdog>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        sheepdogs.Clear();
        sheepdogs.AddRange(discoveredSheepdogs);
        sheepdogs.Sort((a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));

        if (sheepdogs.Count == 0 && sheepdog != null)
        {
            sheepdogs.Add(sheepdog);
        }

        cardSprites.Clear();

        for (int i = 0; i < sheepdogs.Count; i++)
        {
            cardSprites.Add(ResolveCardSprite(sheepdogs[i]));
        }

        if (selectedIndex >= sheepdogs.Count)
        {
            selectedIndex = -1;
        }
    }

    // handles number key toggles for up to 9 sheepdogs
    private void HandleHotkeySelection()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        for (int i = 0; i < sheepdogs.Count && i < 9; i++)
        {
            if (!DidPressDigitKey(keyboard, i))
            {
                continue;
            }

            ToggleSelection(i);
            return;
        }
    }

    // toggles the current selection state for one sheepdog index
    private void ToggleSelection(int index)
    {
        selectedIndex = selectedIndex == index ? -1 : index;
    }

    // checks whether the given hotkey index was pressed this frame
    private bool DidPressDigitKey(Keyboard keyboard, int index)
    {
        return index switch
        {
            0 => keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame,
            1 => keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame,
            2 => keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame,
            3 => keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame,
            4 => keyboard.digit5Key.wasPressedThisFrame || keyboard.numpad5Key.wasPressedThisFrame,
            5 => keyboard.digit6Key.wasPressedThisFrame || keyboard.numpad6Key.wasPressedThisFrame,
            6 => keyboard.digit7Key.wasPressedThisFrame || keyboard.numpad7Key.wasPressedThisFrame,
            7 => keyboard.digit8Key.wasPressedThisFrame || keyboard.numpad8Key.wasPressedThisFrame,
            8 => keyboard.digit9Key.wasPressedThisFrame || keyboard.numpad9Key.wasPressedThisFrame,
            _ => false
        };
    }

    // checks whether placement mode should be cancelled
    private bool DidCancelSelection()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        bool escapePressed = keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
        bool rightClickPressed = mouse != null && mouse.rightButton.wasPressedThisFrame;
        return escapePressed || rightClickPressed;
    }

    // checks whether the player confirmed a sheepdog placement
    private bool DidConfirmPlacement()
    {
        Mouse mouse = Mouse.current;
        return mouse != null && mouse.leftButton.wasPressedThisFrame;
    }

    // reads the current mouse screen position from the input system
    private Vector3 GetMouseScreenPosition()
    {
        Mouse mouse = Mouse.current;
        return mouse != null ? mouse.position.ReadValue() : Vector3.zero;
    }

    // checks whether the cursor is currently over any sheepdog card
    private bool IsPointerOverAnyCard()
    {
        Vector2 mousePosition = GetMouseScreenPosition();
        float guiMouseY = Screen.height - mousePosition.y;

        for (int i = 0; i < sheepdogs.Count; i++)
        {
            if (GetCardRect(i, sheepdogs.Count).Contains(new Vector2(mousePosition.x, guiMouseY)))
            {
                return true;
            }
        }

        return false;
    }

    // computes the gui rect for one card in the centered bottom row
    private Rect GetCardRect(int index, int totalCards)
    {
        float totalWidth = (cardWidth * totalCards) + (cardSpacing * Mathf.Max(0, totalCards - 1));
        float startX = (Screen.width - totalWidth) * 0.5f;

        return new Rect(
            startX + (index * (cardWidth + cardSpacing)),
            Screen.height - cardHeight - cardBottomMargin,
            cardWidth,
            cardHeight
        );
    }

    // resolves the sprite shown on a card, falling back to the sheepdog visual
    private Sprite ResolveCardSprite(Sheepdog currentSheepdog)
    {
        if (cardSprite != null)
        {
            return cardSprite;
        }

        if (currentSheepdog == null)
        {
            return null;
        }

        SpriteRenderer sheepdogRenderer = currentSheepdog.GetComponentInChildren<SpriteRenderer>();
        return sheepdogRenderer != null ? sheepdogRenderer.sprite : null;
    }

    // returns the sprite for a given card index
    private Sprite GetCardSprite(int index)
    {
        if (index < 0 || index >= cardSprites.Count)
        {
            return null;
        }

        return cardSprites[index];
    }

    // draws a simple filled card with an outline so selected state stays obvious
    private void DrawCardBackground(Rect rect, bool isSelected)
    {
        Texture2D outlineTexture = isSelected ? selectedCardOutlineTexture : cardOutlineTexture;
        Texture2D fillTexture = isSelected ? selectedCardFillTexture : cardFillTexture;
        GUI.DrawTexture(rect, outlineTexture, ScaleMode.StretchToFill);
        Rect innerRect = new(rect.x + 3f, rect.y + 3f, rect.width - 6f, rect.height - 6f);
        GUI.DrawTexture(innerRect, fillTexture, ScaleMode.StretchToFill);
    }

    // creates a solid texture used for simple card fills and outlines
    private Texture2D CreateSolidTexture(Color color)
    {
        Texture2D texture = new(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    // builds the number label style used at the top of the card
    private GUIStyle CreateLabelStyle()
    {
        GUIStyle style = new();
        style.alignment = TextAnchor.UpperCenter;
        style.fontSize = 24;
        style.font = GameFonts.Bold;
        style.normal.textColor = Color.white;
        return style;
    }

    // builds the sheepdog name style used above the card sprite
    private GUIStyle CreateNameStyle()
    {
        GUIStyle style = new();
        style.alignment = TextAnchor.LowerCenter;
        style.fontSize = 14;
        style.font = GameFonts.Regular;
        style.normal.textColor = Color.white;
        return style;
    }

    // draws one sprite inside the given gui rect
    private void DrawSprite(Rect rect, Sprite sprite)
    {
        if (sprite.texture == null)
        {
            return;
        }

        Rect textureRect = sprite.textureRect;
        Rect uv = new(
            textureRect.x / sprite.texture.width,
            textureRect.y / sprite.texture.height,
            textureRect.width / sprite.texture.width,
            textureRect.height / sprite.texture.height
        );

        GUI.DrawTextureWithTexCoords(rect, sprite.texture, uv, true);
    }
}
