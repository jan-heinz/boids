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

    private bool isSelected;
    private GUIStyle cardStyle;
    private GUIStyle selectedCardStyle;

    // finds the sheepdog when needed and builds the card styles
    private void Awake()
    {
        if (sheepdog == null)
        {
            sheepdog = FindFirstObjectByType<Sheepdog>();
        }

        cardStyle = CreateCardStyle(new Color(0.15f, 0.14f, 0.12f, 0.95f));
        selectedCardStyle = CreateCardStyle(new Color(0.62f, 0.43f, 0.14f, 0.95f));
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
        float width = 120f;
        float height = 72f;
        Rect cardRect = new(
            (Screen.width * 0.5f) - (width * 0.5f),
            Screen.height - height - 24f,
            width,
            height
        );
        string status = sheepdog != null && sheepdog.IsPlaced ? "move" : "place";
        string cardText = $"{cardLabel}\n{status}";

        if (GUI.Button(cardRect, GUIContent.none, isSelected ? selectedCardStyle : cardStyle))
        {
            isSelected = true;
        }

        GUI.Box(cardRect, GUIContent.none, isSelected ? selectedCardStyle : cardStyle);
        GUI.Label(cardRect, cardText, isSelected ? selectedCardStyle : cardStyle);
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
        style.normal.background = backgroundTexture;
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
}
