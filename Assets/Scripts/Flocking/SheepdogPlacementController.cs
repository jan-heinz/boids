using UnityEngine;
using UnityEngine.InputSystem;

// handles selecting and moving one sheepdog
public class SheepdogPlacementController : MonoBehaviour
{
    // -------------------------------------------------------------------------------------------------------------

    [SerializeField]
    [Tooltip("Sheepdog this controller should place and move")]
    private Sheepdog sheepdog;

    [SerializeField]
    [Tooltip("Label shown on the sheepdog card")]
    private string cardLabel = "1";

    [SerializeField]
    [Tooltip("Optional sprite shown on the sheepdog card")]
    private Sprite cardSprite;

    [SerializeField, Min(1f)]
    [Tooltip("Width of the sheepdog card")]
    private float cardWidth = 88f;

    [SerializeField, Min(1f)]
    [Tooltip("Height of the sheepdog card")]
    private float cardHeight = 96f;

    [SerializeField, Min(0f)]
    [Tooltip("Padding above the card from the bottom of the screen")]
    private float cardBottomMargin = 24f;

    [SerializeField, Min(1f)]
    [Tooltip("Width of the dog sprite inside the card")]
    private float cardSpriteWidth = 56f;

    [SerializeField, Min(1f)]
    [Tooltip("Height of the dog sprite inside the card")]
    private float cardSpriteHeight = 48f;

    private bool isSelected;
    private GUIStyle cardStyle;
    private GUIStyle selectedCardStyle;
    private GUIStyle cardLabelStyle;
    private GUIStyle cardNameStyle;

    // finds the sheepdog when needed and builds the card styles
    private void Awake()
    {
        if (sheepdog == null)
        {
            sheepdog = FindFirstObjectByType<Sheepdog>();
        }

        cardStyle = CreateCardStyle(new Color(0.15f, 0.14f, 0.12f, 0.95f));
        selectedCardStyle = CreateCardStyle(new Color(0.62f, 0.43f, 0.14f, 0.95f));
        cardLabelStyle = CreateLabelStyle();
        cardNameStyle = CreateNameStyle();

        if (cardSprite == null && sheepdog != null)
        {
            SpriteRenderer sheepdogRenderer = sheepdog.GetComponentInChildren<SpriteRenderer>();

            if (sheepdogRenderer != null)
            {
                cardSprite = sheepdogRenderer.sprite;
            }
        }
    }

    // listens for number key selection and click movement orders
    private void Update()
    {
        if (DidPressSelectKey())
        {
            isSelected = !isSelected;
        }

        if (!isSelected)
        {
            return;
        }

        if (DidCancelSelection())
        {
            isSelected = false;
            return;
        }

        if (!DidConfirmPlacement())
        {
            return;
        }

        Camera cameraToUse = Camera.main;

        if (cameraToUse == null || sheepdog == null)
        {
            return;
        }

        Vector3 mousePosition = GetMouseScreenPosition();
        Vector3 worldPosition = cameraToUse.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, -cameraToUse.transform.position.z));
        sheepdog.MoveTo(worldPosition);
    }

    // draws a simple card for selecting the sheepdog
    private void OnGUI()
    {
        Rect cardRect = new(
            (Screen.width * 0.5f) - (cardWidth * 0.5f),
            Screen.height - cardHeight - cardBottomMargin,
            cardWidth,
            cardHeight
        );
        GUIStyle activeStyle = isSelected ? selectedCardStyle : cardStyle;

        if (GUI.Button(cardRect, GUIContent.none, activeStyle))
        {
            isSelected = !isSelected;
        }

        GUI.Box(cardRect, GUIContent.none, activeStyle);

        Rect labelRect = new(cardRect.x, cardRect.y + 8f, cardRect.width, 28f);
        GUI.Label(labelRect, cardLabel, cardLabelStyle);

        if (sheepdog == null)
        {
            return;
        }

        Rect nameRect = new(cardRect.x + 6f, cardRect.y + 34f, cardRect.width - 12f, 18f);
        GUI.Label(nameRect, sheepdog.DisplayName, cardNameStyle);

        if (cardSprite == null)
        {
            return;
        }

        Rect spriteRect = new(
            cardRect.x + ((cardRect.width - cardSpriteWidth) * 0.5f),
            cardRect.y + 52f,
            cardSpriteWidth,
            cardSpriteHeight
        );
        DrawSprite(spriteRect, cardSprite);
    }

    // builds a card style with the given background color
    private GUIStyle CreateCardStyle(Color backgroundColor)
    {
        Texture2D backgroundTexture = new(1, 1);
        backgroundTexture.SetPixel(0, 0, backgroundColor);
        backgroundTexture.Apply();

        GUIStyle style = new();
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = 24;
        style.font = GameFonts.Bold;
        style.normal.background = backgroundTexture;
        style.normal.textColor = Color.white;
        return style;
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

    // builds the sheepdog name style used at the bottom of the card
    private GUIStyle CreateNameStyle()
    {
        GUIStyle style = new();
        style.alignment = TextAnchor.LowerCenter;
        style.fontSize = 14;
        style.font = GameFonts.Regular;
        style.normal.textColor = Color.white;
        return style;
    }

    // checks whether the sheepdog hotkey was pressed this frame
    private bool DidPressSelectKey()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return false;
        }

        return keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame;
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

    // reads the current mouse screen position from the active input backend
    private Vector3 GetMouseScreenPosition()
    {
        Mouse mouse = Mouse.current;
        return mouse != null ? mouse.position.ReadValue() : Vector3.zero;
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
