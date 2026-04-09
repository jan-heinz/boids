using UnityEngine;
using UnityEngine.InputSystem;

// pauses the level and shows a dismissible intro message
public class LevelIntroOverlay : MonoBehaviour
{
    // -------------------------------------------------------------------------------------------------------------

    [SerializeField]
    [Tooltip("Header shown at the top of the tutorial overlay")]
    private string titleText = "How to Play";

    [SerializeField, TextArea(4, 8)]
    [Tooltip("Body text shown in the tutorial overlay")]
    private string bodyText =
        "Explain the level here.\n\n" +
        "Tell the player how to move the sheepdogs and what to save.";

    [SerializeField]
    [Tooltip("Hint shown under the tutorial text")]
    private string continueText = "Click or press Space to start";

    [Header("Layout")]
    [SerializeField]
    [Tooltip("How wide the tutorial panel should be")]
    private float panelWidth = 1120f;

    [SerializeField]
    [Tooltip("How tall the tutorial panel should be")]
    private float panelHeight = 700f;

    [SerializeField]
    [Tooltip("Horizontal center offset for the tutorial panel")]
    private float panelOffsetX = 0f;

    [SerializeField]
    [Tooltip("Vertical center offset for the tutorial panel")]
    private float panelOffsetY = -70f;

    [SerializeField]
    [Tooltip("Top padding used by the title inside the panel")]
    private float titleTop = 18f;

    [SerializeField]
    [Tooltip("Top padding used by the body text inside the panel")]
    private float bodyTop = 78f;

    [SerializeField]
    [Tooltip("Height used by the body text area")]
    private float bodyHeight = 470f;

    [SerializeField]
    [Tooltip("Distance from the panel bottom to the continue prompt")]
    private float continueBottom = 46f;

    [SerializeField]
    [Tooltip("Whether the level should pause while the tutorial is visible")]
    private bool pauseWhileVisible = true;

    [SerializeField]
    [Tooltip("Dark tint drawn behind the tutorial box")]
    private Color backdropColor = new(0f, 0f, 0f, 0.5f);

    [SerializeField]
    [Tooltip("Panel color drawn behind the tutorial text")]
    private Color panelColor = new(0.13f, 0.11f, 0.08f, 0.94f);

    [SerializeField]
    [Tooltip("Title color")]
    private Color titleColor = Color.white;

    [SerializeField]
    [Tooltip("Body text color")]
    private Color bodyColor = Color.white;

    [SerializeField]
    [Tooltip("Continue hint color")]
    private Color continueColor = new(1f, 0.95f, 0.82f, 1f);

    private bool isVisible = true;
    private GUIStyle titleStyle;
    private GUIStyle bodyStyle;
    private GUIStyle continueStyle;
    private Texture2D backdropTexture;
    private Texture2D panelTexture;

    // builds the overlay styles and pauses the level
    private void Awake()
    {
        titleStyle = CreateTextStyle(42, titleColor, GameFonts.Bold, TextAnchor.MiddleCenter);
        bodyStyle = CreateTextStyle(24, bodyColor, GameFonts.Regular, TextAnchor.UpperLeft);
        continueStyle = CreateTextStyle(20, continueColor, GameFonts.Regular, TextAnchor.MiddleCenter);
        backdropTexture = CreateColorTexture(backdropColor);
        panelTexture = CreateColorTexture(panelColor);

        if (pauseWhileVisible)
        {
            Time.timeScale = 0f;
        }
    }

    // waits for the player to dismiss the overlay
    private void Update()
    {
        if (!isVisible)
        {
            return;
        }

        Mouse mouse = Mouse.current;
        Keyboard keyboard = Keyboard.current;
        bool clicked = mouse != null && mouse.leftButton.wasPressedThisFrame;
        bool pressedKey = keyboard != null
            && (keyboard.spaceKey.wasPressedThisFrame
                || keyboard.enterKey.wasPressedThisFrame
                || keyboard.numpadEnterKey.wasPressedThisFrame);

        if (!clicked && !pressedKey)
        {
            return;
        }

        isVisible = false;

        if (pauseWhileVisible)
        {
            Time.timeScale = 1f;
        }
    }

    // draws the centered tutorial overlay
    private void OnGUI()
    {
        if (!isVisible)
        {
            return;
        }

        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), backdropTexture);

        Rect panelRect = new(
            (Screen.width * 0.5f) - (panelWidth * 0.5f) + panelOffsetX,
            (Screen.height * 0.5f) - (panelHeight * 0.5f) + panelOffsetY,
            panelWidth,
            panelHeight
        );
        GUI.DrawTexture(panelRect, panelTexture);

        Rect titleRect = new(panelRect.x + 24f, panelRect.y + titleTop, panelRect.width - 48f, 56f);
        GUI.Label(titleRect, titleText, titleStyle);

        Rect bodyRect = new(panelRect.x + 32f, panelRect.y + bodyTop, panelRect.width - 64f, bodyHeight);
        GUI.Label(bodyRect, bodyText, bodyStyle);

        Rect continueRect = new(panelRect.x + 24f, panelRect.yMax - continueBottom, panelRect.width - 48f, 32f);
        GUI.Label(continueRect, continueText, continueStyle);
    }

    // builds one gui text style
    private GUIStyle CreateTextStyle(int fontSize, Color color, Font font, TextAnchor alignment)
    {
        GUIStyle style = new();
        style.font = font;
        style.fontSize = fontSize;
        style.alignment = alignment;
        style.wordWrap = true;
        style.normal.textColor = color;
        return style;
    }

    // builds a 1x1 texture in one color
    private Texture2D CreateColorTexture(Color color)
    {
        Texture2D texture = new(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }
}
