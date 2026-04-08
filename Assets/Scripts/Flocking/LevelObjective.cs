using UnityEngine;

// tracks the timer and win loss state for one level
public class LevelObjective : MonoBehaviour
{
    // -------------------------------------------------------------------------------------------------------------

    [SerializeField]
    [Tooltip("Goal area used to read how many sheep are safe")]
    private GoalArea goalArea;

    [Header("Objective")]
    [SerializeField, Min(1f)]
    [Tooltip("How many seconds the player has to finish the level")]
    private float levelDuration = 60f;

    [SerializeField, Range(0f, 1f)]
    [Tooltip("What percent of the flock must reach the goal to win")]
    private float requiredSafePercent = 0.8f;

    [SerializeField]
    [Tooltip("Whether the game should pause after the level ends")]
    private bool pauseOnEnd = true;

    [Header("Hud")]
    [SerializeField, Min(1)]
    [Tooltip("How large the timer and requirement text should be")]
    private int hudFontSize = 28;

    [SerializeField, Min(1)]
    [Tooltip("How large the end state text should be")]
    private int resultFontSize = 56;

    [SerializeField]
    [Tooltip("Color used for the timer and requirement text")]
    private Color hudColor = Color.white;

    [SerializeField]
    [Tooltip("Color used for the win text")]
    private Color winColor = new(0.6f, 1f, 0.6f, 1f);

    [SerializeField]
    [Tooltip("Color used for the lose text")]
    private Color loseColor = new(1f, 0.55f, 0.55f, 1f);

    private float timeRemaining;
    private bool isComplete;
    private bool didWin;
    private GUIStyle hudStyle;
    private GUIStyle resultStyle;

    public float TimeRemaining => timeRemaining;
    public bool DidWin => isComplete && didWin;
    public bool DidLose => isComplete && !didWin;
    public int RequiredSafeSheepCount => GetRequiredSafeSheepCount();

    // finds references and resets the level timer
    private void Awake()
    {
        if (goalArea == null)
        {
            goalArea = FindFirstObjectByType<GoalArea>();
        }

        Time.timeScale = 1f;
        timeRemaining = levelDuration;
        hudStyle = CreateStyle(hudFontSize, hudColor);
        resultStyle = CreateStyle(resultFontSize, Color.white);
    }

    // updates the timer and checks for win loss state changes
    private void Update()
    {
        if (isComplete || goalArea == null)
        {
            return;
        }

        if (goalArea.SafeSheepCount >= GetRequiredSafeSheepCount())
        {
            CompleteLevel(true);
            return;
        }

        timeRemaining -= Time.deltaTime;

        if (timeRemaining > 0f)
        {
            return;
        }

        timeRemaining = 0f;
        CompleteLevel(false);
    }

    // draws the timer requirement and end state overlay
    private void OnGUI()
    {
        if (goalArea == null)
        {
            return;
        }

        float width = 320f;
        float height = 72f;
        Rect hudRect = new(
            (Screen.width * 0.5f) - (width * 0.5f),
            24f,
            width,
            height
        );
        string hudText =
            $"Time {Mathf.CeilToInt(timeRemaining)}\n" +
            $"Safe {goalArea.SafeSheepCount}/{RequiredSafeSheepCount}";

        GUI.Label(hudRect, hudText, hudStyle);

        if (!isComplete)
        {
            return;
        }

        resultStyle.normal.textColor = didWin ? winColor : loseColor;
        Rect resultRect = new(
            0f,
            (Screen.height * 0.5f) - 60f,
            Screen.width,
            120f
        );
        GUI.Label(resultRect, didWin ? "You Win" : "You Lose", resultStyle);
    }

    // builds a centered gui style for the overlay text
    private GUIStyle CreateStyle(int fontSize, Color color)
    {
        GUIStyle style = new();
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = fontSize;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = color;
        return style;
    }

    // converts the percent requirement into a sheep count
    private int GetRequiredSafeSheepCount()
    {
        int totalSheepCount = goalArea != null ? goalArea.TotalSheepCount : 0;

        if (totalSheepCount <= 0)
        {
            return 0;
        }

        return Mathf.Clamp(
            Mathf.CeilToInt(totalSheepCount * requiredSafePercent),
            1,
            totalSheepCount
        );
    }

    // ends the level and optionally pauses the game
    private void CompleteLevel(bool won)
    {
        isComplete = true;
        didWin = won;

        if (!pauseOnEnd)
        {
            return;
        }

        Time.timeScale = 0f;
    }
}
