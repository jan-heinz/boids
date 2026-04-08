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

    [Header("Star Hud")]
    [SerializeField]
    [Tooltip("Bronze star sprite used for the 1 star requirement")]
    private Sprite bronzeStar;

    [SerializeField]
    [Tooltip("Silver star sprite used for the 2 star requirement")]
    private Sprite silverStar;

    [SerializeField]
    [Tooltip("Gold star sprite used for the 3 star requirement")]
    private Sprite goldStar;

    [SerializeField, Min(1)]
    [Tooltip("How large the star requirement numbers should be")]
    private int starCountFontSize = 22;

    [SerializeField, Min(1f)]
    [Tooltip("How large each star icon should be on screen")]
    private float starIconSize = 36f;

    [SerializeField, Min(0f)]
    [Tooltip("How much space should sit between the star columns")]
    private float starColumnSpacing = 20f;

    [SerializeField]
    [Tooltip("Color used for the star requirement numbers")]
    private Color starCountColor = Color.white;

    [SerializeField, Min(1)]
    [Tooltip("How large the saved count text should be on the end screen")]
    private int savedCountFontSize = 28;

    [SerializeField, Min(1f)]
    [Tooltip("How large each earned star should be on the end screen")]
    private float resultStarIconSize = 56f;

    [SerializeField, Min(0f)]
    [Tooltip("How much space should sit between earned stars on the end screen")]
    private float resultStarSpacing = 16f;

    private float timeRemaining;
    private bool isComplete;
    private bool didWin;
    private int earnedStars;
    private GUIStyle hudStyle;
    private GUIStyle resultStyle;
    private GUIStyle starCountStyle;
    private GUIStyle savedCountStyle;

    public float TimeRemaining => timeRemaining;
    public bool DidWin => isComplete && didWin;
    public bool DidLose => isComplete && !didWin;
    public int EarnedStars => earnedStars;
    public int OneStarSafeSheepCount => GetSafeSheepCountForPercent(0.8f);
    public int TwoStarSafeSheepCount => GetSafeSheepCountForPercent(0.9f);
    public int ThreeStarSafeSheepCount => goalArea != null ? goalArea.TotalSheepCount : 0;

    // finds references and resets the level timer
    private void Awake()
    {
        if (goalArea == null)
        {
            goalArea = FindFirstObjectByType<GoalArea>();
        }

        Time.timeScale = 1f;
        timeRemaining = levelDuration;
        hudStyle = CreateStyle(hudFontSize, hudColor, false);
        resultStyle = CreateStyle(resultFontSize, Color.white, false);
        starCountStyle = CreateStyle(starCountFontSize, starCountColor, false);
        savedCountStyle = CreateStyle(savedCountFontSize, Color.white, false);
    }

    // updates the timer and checks for win loss state changes
    private void Update()
    {
        if (isComplete || goalArea == null)
        {
            return;
        }

        if (goalArea.SafeSheepCount >= ThreeStarSafeSheepCount)
        {
            CompleteLevel(GetStarRating(goalArea.SafeSheepCount));
            return;
        }

        timeRemaining -= Time.deltaTime;

        if (timeRemaining > 0f)
        {
            return;
        }

        timeRemaining = 0f;
        CompleteLevel(GetStarRating(goalArea.SafeSheepCount));
    }

    // draws the timer requirement and end state overlay
    private void OnGUI()
    {
        if (goalArea == null)
        {
            return;
        }

        float width = 240f;
        float height = 48f;
        Rect timerRect = new(
            (Screen.width * 0.5f) - (width * 0.5f),
            24f,
            width,
            height
        );
        GUI.Label(timerRect, $"Time {Mathf.CeilToInt(timeRemaining)}", hudStyle);

        DrawStarRequirementHud();

        if (!isComplete)
        {
            return;
        }

        DrawResultOverlay();
    }

    // draws the final result panel once the level has ended
    private void DrawResultOverlay()
    {
        resultStyle.normal.textColor = didWin ? winColor : loseColor;

        if (!didWin)
        {
            Rect loseRect = new(0f, (Screen.height * 0.5f) - 20f, Screen.width, 80f);
            GUI.Label(loseRect, "You Lose", resultStyle);
            return;
        }

        DrawEarnedStars();

        Rect resultRect = new(0f, (Screen.height * 0.5f) - 20f, Screen.width, 80f);
        GUI.Label(resultRect, "You Win", resultStyle);

        Rect savedRect = new(0f, (Screen.height * 0.5f) + 46f, Screen.width, 50f);
        GUI.Label(savedRect, $"{goalArea.SafeSheepCount}/{goalArea.TotalSheepCount} Saved", savedCountStyle);
    }

    // draws the three star requirements in the top right corner
    private void DrawStarRequirementHud()
    {
        if (bronzeStar == null || silverStar == null || goldStar == null)
        {
            return;
        }

        float columnWidth = starIconSize + starColumnSpacing;
        float totalWidth = (columnWidth * 3f) - starColumnSpacing;
        float startX = Screen.width - totalWidth - 24f;
        float iconY = 24f;

        DrawStarRequirementColumn(bronzeStar.texture, startX, iconY, OneStarSafeSheepCount);
        DrawStarRequirementColumn(silverStar.texture, startX + columnWidth, iconY, TwoStarSafeSheepCount);
        DrawStarRequirementColumn(goldStar.texture, startX + (columnWidth * 2f), iconY, ThreeStarSafeSheepCount);
    }

    // draws one star icon with its sheep requirement underneath
    private void DrawStarRequirementColumn(Texture texture, float x, float iconY, int requirement)
    {
        if (texture == null)
        {
            return;
        }

        Rect iconRect = new(x, iconY, starIconSize, starIconSize);
        GUI.DrawTexture(iconRect, texture, ScaleMode.ScaleToFit, true);

        Rect textRect = new(x - 12f, iconY + starIconSize + 4f, starIconSize + 24f, starCountFontSize + 8f);
        GUI.Label(textRect, requirement.ToString(), starCountStyle);
    }

    // draws the earned stars above the win text
    private void DrawEarnedStars()
    {
        Texture starTexture = GetEarnedStarTexture();

        if (starTexture == null || earnedStars <= 0)
        {
            return;
        }

        float totalWidth = (resultStarIconSize * earnedStars) + (resultStarSpacing * Mathf.Max(0, earnedStars - 1));
        float startX = (Screen.width - totalWidth) * 0.5f;
        float y = (Screen.height * 0.5f) - 92f;

        for (int i = 0; i < earnedStars; i++)
        {
            Rect iconRect = new(
                startX + (i * (resultStarIconSize + resultStarSpacing)),
                y,
                resultStarIconSize,
                resultStarIconSize
            );
            GUI.DrawTexture(iconRect, starTexture, ScaleMode.ScaleToFit, true);
        }
    }

    // gets the star sprite that matches the earned rank
    private Texture GetEarnedStarTexture()
    {
        return earnedStars switch
        {
            1 => bronzeStar != null ? bronzeStar.texture : null,
            2 => silverStar != null ? silverStar.texture : null,
            3 => goldStar != null ? goldStar.texture : null,
            _ => null
        };
    }

    // builds a centered gui style for the overlay text
    private GUIStyle CreateStyle(int fontSize, Color color, bool useBold = true)
    {
        GUIStyle style = new();
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = fontSize;
        style.fontStyle = useBold ? FontStyle.Bold : FontStyle.Normal;
        style.font = useBold ? GameFonts.Bold : GameFonts.Regular;
        style.normal.textColor = color;
        return style;
    }

    // converts a percent requirement into a sheep count
    private int GetSafeSheepCountForPercent(float percent)
    {
        int totalSheepCount = goalArea != null ? goalArea.TotalSheepCount : 0;

        if (totalSheepCount <= 0)
        {
            return 0;
        }

        return Mathf.Clamp(
            Mathf.CeilToInt(totalSheepCount * percent),
            1,
            totalSheepCount
        );
    }

    // converts the current safe count into a 0 to 3 star rating
    private int GetStarRating(int safeSheepCount)
    {
        if (goalArea == null || goalArea.TotalSheepCount <= 0)
        {
            return 0;
        }

        if (safeSheepCount >= ThreeStarSafeSheepCount)
        {
            return 3;
        }

        if (safeSheepCount >= TwoStarSafeSheepCount)
        {
            return 2;
        }

        if (safeSheepCount >= OneStarSafeSheepCount)
        {
            return 1;
        }

        return 0;
    }

    // ends the level and optionally pauses the game
    private void CompleteLevel(int stars)
    {
        isComplete = true;
        earnedStars = stars;
        didWin = stars > 0;

        if (!pauseOnEnd)
        {
            return;
        }

        Time.timeScale = 0f;
    }
}
