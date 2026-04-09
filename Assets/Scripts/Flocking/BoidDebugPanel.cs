using UnityEngine;

// draws a small in game panel for tuning boid settings during play
public class BoidDebugPanel : MonoBehaviour
{
    // legacy debug flags stay available so existing sheep and wolf debug hooks compile,
    // but the panel no longer exposes or enables them.
    public static bool ShowObstacleCheckRadius => false;
    public static bool ShowNeighborRadius => false;
    public static bool ShowLogPanel => false;
    public static bool LoggingEnabled => false;
    public static float LogInterval => 0.5f;
    public static bool LogFenceBehavior => false;
    public static bool LogBoidBehavior => false;
    public static bool LogWolfBehavior => false;
    public static bool LogMovementState => false;
    public static bool LogWolfMovementState => false;
    public static bool LogNeighborState => false;
    public static bool LogBoidSummary => false;
    public static bool LogWolfTargetState => false;
    public static bool LogWolfRetreatState => false;
    public static bool LogWolfObstacleState => false;
    public static bool LogSeparationForce => false;
    public static bool LogAlignmentForce => false;
    public static bool LogCohesionForce => false;
    public static bool LogBoidSteeringForce => false;
    public static bool LogFrontHit => false;
    public static bool LogLeftHit => false;
    public static bool LogRightHit => false;
    public static bool LogCombinedNormal => false;
    public static bool LogWallPressure => false;
    public static bool LogCornerState => false;
    public static bool LogStuckTime => false;
    public static bool LogDesiredDirection => false;
    public static bool LogSteeringForce => false;
    public static bool LogOnlyActiveSheep => false;
    public static string LogFilePath => null;
    public static bool ShowWolfStateLabel { get; private set; }

    [Header("References")]
    [SerializeField]
    [Tooltip("Boid settings asset the panel should edit")]
    private BoidSettings settings;

    [Header("Layout")]
    [SerializeField]
    [Tooltip("Screen position of the panel from the top left corner")]
    private Vector2 panelPosition = new(16f, 16f);

    [SerializeField]
    [Tooltip("Width and height of the panel")]
    private Vector2 panelSize = new(320f, 300f);

    [SerializeField]
    [Tooltip("Whether the panel should start expanded")]
    private bool startExpanded;

    [SerializeField]
    [Tooltip("Whether wolf state letters should start visible")]
    private bool showWolfStateLabel;

    private float defaultMoveSpeed;
    private float defaultMaxSteeringForce;
    private float defaultNeighborRadius;
    private float defaultSeparationRadius;
    private float defaultObstacleCheckRadius;
    private float defaultSeparationWeight;
    private float defaultAlignmentWeight;
    private float defaultCohesionWeight;
    private float defaultBoundsWeight;
    private bool isExpanded;

    // caches the inspector values so each slider can reset back to them during play
    private void Start()
    {
        isExpanded = startExpanded;

        if (settings == null)
        {
            return;
        }

        defaultMoveSpeed = settings.MoveSpeed;
        defaultMaxSteeringForce = settings.MaxSteeringForce;
        defaultNeighborRadius = settings.NeighborRadius;
        defaultSeparationRadius = settings.SeparationRadius;
        defaultObstacleCheckRadius = settings.ObstacleCheckRadius;
        defaultSeparationWeight = settings.SeparationWeight;
        defaultAlignmentWeight = settings.AlignmentWeight;
        defaultCohesionWeight = settings.CohesionWeight;
        defaultBoundsWeight = settings.BoundsWeight;
        ShowWolfStateLabel = showWolfStateLabel;
    }

    // draws the debug panel each frame
    private void OnGUI()
    {
        float panelHeight = isExpanded ? panelSize.y : 44f;
        Rect panelRect = new(panelPosition.x, panelPosition.y, panelSize.x, panelHeight);
        GUILayout.BeginArea(panelRect, GUI.skin.box);

        if (GUILayout.Button($"{(isExpanded ? "▼" : "▶")} boid settings", GUI.skin.label))
        {
            isExpanded = !isExpanded;
        }

        if (!isExpanded)
        {
            GUILayout.EndArea();
            return;
        }

        if (settings == null)
        {
            GUILayout.EndArea();
            return;
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("reset all", GUILayout.Width(72f)))
        {
            ResetAll();
        }

        GUILayout.EndHorizontal();
        GUILayout.Space(8f);

        DrawSlider("move speed", settings.MoveSpeed, defaultMoveSpeed, 0f, 10f, value => settings.MoveSpeed = value);
        DrawSlider("max steering force", settings.MaxSteeringForce, defaultMaxSteeringForce, 0f, 20f, value => settings.MaxSteeringForce = value);

        GUILayout.Space(8f);

        DrawSlider("neighbor radius", settings.NeighborRadius, defaultNeighborRadius, 0f, 10f, value => settings.NeighborRadius = value);
        DrawSlider("separation radius", settings.SeparationRadius, defaultSeparationRadius, 0f, 10f, value => settings.SeparationRadius = value);
        DrawSlider("obstacle check radius", settings.ObstacleCheckRadius, defaultObstacleCheckRadius, 0f, 10f, value => settings.ObstacleCheckRadius = value);

        GUILayout.Space(8f);

        DrawSlider("separation weight", settings.SeparationWeight, defaultSeparationWeight, 0f, 10f, value => settings.SeparationWeight = value);
        DrawSlider("alignment weight", settings.AlignmentWeight, defaultAlignmentWeight, 0f, 10f, value => settings.AlignmentWeight = value);
        DrawSlider("cohesion weight", settings.CohesionWeight, defaultCohesionWeight, 0f, 10f, value => settings.CohesionWeight = value);
        DrawSlider("bounds weight", settings.BoundsWeight, defaultBoundsWeight, 0f, 10f, value => settings.BoundsWeight = value);

        GUILayout.Space(8f);
        showWolfStateLabel = GUILayout.Toggle(showWolfStateLabel, "show wolf state");
        ShowWolfStateLabel = showWolfStateLabel;

        GUILayout.EndArea();
    }

    // draws one labeled slider row and lets the user reset back to the cached default
    private void DrawSlider(string label, float currentValue, float defaultValue, float minValue, float maxValue, System.Action<float> setValue)
    {
        GUILayout.Label($"{label}: {currentValue:0.00}");
        GUILayout.BeginHorizontal();

        float nextValue = GUILayout.HorizontalSlider(currentValue, minValue, maxValue);
        setValue(nextValue);

        if (GUILayout.Button("reset", GUILayout.Width(56f)))
        {
            setValue(defaultValue);
        }

        GUILayout.EndHorizontal();
    }

    // resets every setting back to the values cached at play start
    private void ResetAll()
    {
        settings.MoveSpeed = defaultMoveSpeed;
        settings.MaxSteeringForce = defaultMaxSteeringForce;
        settings.NeighborRadius = defaultNeighborRadius;
        settings.SeparationRadius = defaultSeparationRadius;
        settings.ObstacleCheckRadius = defaultObstacleCheckRadius;
        settings.SeparationWeight = defaultSeparationWeight;
        settings.AlignmentWeight = defaultAlignmentWeight;
        settings.CohesionWeight = defaultCohesionWeight;
        settings.BoundsWeight = defaultBoundsWeight;
    }

    // kept for compatibility with older debug hooks
    public static void AppendLogLine(string line)
    {
    }
}
