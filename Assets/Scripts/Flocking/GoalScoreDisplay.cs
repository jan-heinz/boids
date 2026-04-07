using UnityEngine;

// draws the safe sheep count over the goal area
public class GoalScoreDisplay : MonoBehaviour
{
    // -------------------------------------------------------------------------------------------------------------

    [SerializeField]
    [Tooltip("Goal area this score should read from")]
    private GoalArea goalArea;

    [SerializeField]
    [Tooltip("World offset from the goal center for the score")]
    private Vector3 worldOffset = Vector3.zero;

    [SerializeField, Min(1)]
    [Tooltip("Font size used by the score")]
    private int fontSize = 48;

    [SerializeField]
    [Tooltip("Color used by the score")]
    private Color textColor = Color.white;

    private GUIStyle scoreStyle;

    // builds the screen label style and finds the goal area when needed
    private void Awake()
    {
        if (goalArea == null)
        {
            goalArea = GetComponent<GoalArea>();
        }

        if (goalArea == null)
        {
            goalArea = FindFirstObjectByType<GoalArea>();
        }

        scoreStyle = new GUIStyle
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = fontSize,
            normal =
            {
                textColor = textColor
            }
        };
    }

    // draws the score over the goal area in screen space
    private void OnGUI()
    {
        if (goalArea == null)
        {
            return;
        }

        Camera cameraToUse = Camera.main;

        if (cameraToUse == null)
        {
            return;
        }

        Vector3 screenPosition = cameraToUse.WorldToScreenPoint(goalArea.Center + (Vector2)worldOffset);

        if (screenPosition.z <= 0f)
        {
            return;
        }

        float width = 240f;
        float height = fontSize * 1.5f;
        Rect labelRect = new(
            screenPosition.x - (width * 0.5f),
            Screen.height - screenPosition.y - (height * 0.5f),
            width,
            height
        );

        GUI.Label(labelRect, goalArea.ScoreText, scoreStyle);
    }
}
