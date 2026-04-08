using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

// draws a small in game panel for tuning boid settings during play
public class BoidDebugPanel : MonoBehaviour
{
    public static bool ShowObstacleCheckRadius { get; private set; }
    public static bool ShowNeighborRadius { get; private set; }
    public static bool ShowLogPanel { get; private set; }
    public static bool LoggingEnabled { get; private set; }
    public static float LogInterval { get; private set; } = 0.5f;
    public static bool LogFenceBehavior { get; private set; }
    public static bool LogBoidBehavior { get; private set; }
    public static bool LogWolfBehavior { get; private set; }
    public static bool LogMovementState { get; private set; }
    public static bool LogWolfMovementState { get; private set; }
    public static bool LogNeighborState { get; private set; }
    public static bool LogBoidSummary { get; private set; }
    public static bool LogWolfTargetState { get; private set; }
    public static bool LogWolfRetreatState { get; private set; }
    public static bool LogWolfObstacleState { get; private set; }
    public static bool LogSeparationForce { get; private set; }
    public static bool LogAlignmentForce { get; private set; }
    public static bool LogCohesionForce { get; private set; }
    public static bool LogBoidSteeringForce { get; private set; }
    public static bool LogFrontHit { get; private set; }
    public static bool LogLeftHit { get; private set; }
    public static bool LogRightHit { get; private set; }
    public static bool LogCombinedNormal { get; private set; }
    public static bool LogWallPressure { get; private set; }
    public static bool LogCornerState { get; private set; }
    public static bool LogStuckTime { get; private set; }
    public static bool LogDesiredDirection { get; private set; }
    public static bool LogSteeringForce { get; private set; }
    public static bool LogOnlyActiveSheep { get; private set; }
    public static string LogFilePath { get; private set; }

    // -------------------------------------------------------------------------------------------------------------

    [Header("References")]
    [SerializeField]
    [Tooltip("Boid settings asset the panel should edit")]
    private BoidSettings settings;

    [SerializeField]
    [Tooltip("Flock manager the panel should read sheep logs from")]
    private FlockManager flockManager;

    // -------------------------------------------------------------------------------------------------------------

    [Header("Layout")]
    [SerializeField]
    [Tooltip("Screen position of the panel from the top left corner")]
    private Vector2 panelPosition = new Vector2(16f, 16f);

    [SerializeField]
    [Tooltip("Width and height of the panel")]
    private Vector2 panelSize = new Vector2(320f, 420f);

    [SerializeField]
    [Tooltip("Offset from the main panel to the log panel")]
    private Vector2 logPanelOffset = new Vector2(336f, 0f);

    [SerializeField]
    [Tooltip("Width and height of the log panel")]
    private Vector2 logPanelSize = new Vector2(320f, 360f);

    // -------------------------------------------------------------------------------------------------------------

    [Header("Log Defaults")]
    [SerializeField]
    [Tooltip("Whether logging should start as soon as play mode begins")]
    private bool loggingEnabled;

    [SerializeField]
    [Tooltip("How often the panel should write one flock log sample")]
    private float logInterval = 0.5f;

    [SerializeField]
    [Tooltip("Whether fence and obstacle response logs should be written to the log file")]
    private bool logFenceBehavior = true;

    [SerializeField]
    [Tooltip("Whether flocking and boid response logs should be written to the log file")]
    private bool logBoidBehavior;

    [SerializeField]
    [Tooltip("Whether wolf behavior logs should be written to the log file")]
    private bool logWolfBehavior = true;

    [SerializeField]
    [Tooltip("Whether movement values should be written to the log file")]
    private bool logMovementState;

    [SerializeField]
    [Tooltip("Whether wolf movement values should be written to the log file")]
    private bool logWolfMovementState = true;

    [SerializeField]
    [Tooltip("Whether nearby flockmate counts should be written to the log file")]
    private bool logNeighborState;

    [SerializeField]
    [Tooltip("Whether a compact boid summary should be written to the log file")]
    private bool logBoidSummary = true;

    [SerializeField]
    [Tooltip("Whether wolf target selection should be written to the log file")]
    private bool logWolfTargetState = true;

    [SerializeField]
    [Tooltip("Whether wolf retreat values should be written to the log file")]
    private bool logWolfRetreatState = true;

    [SerializeField]
    [Tooltip("Whether wolf obstacle response should be written to the log file")]
    private bool logWolfObstacleState = true;

    [SerializeField]
    [Tooltip("Whether separation steering should be written to the log file")]
    private bool logSeparationForce;

    [SerializeField]
    [Tooltip("Whether alignment steering should be written to the log file")]
    private bool logAlignmentForce;

    [SerializeField]
    [Tooltip("Whether cohesion steering should be written to the log file")]
    private bool logCohesionForce;

    [SerializeField]
    [Tooltip("Whether total boid steering should be written to the log file")]
    private bool logBoidSteeringForce;

    [SerializeField]
    [Tooltip("Whether the front obstacle cast should be written to the log file")]
    private bool logFrontHit;

    [SerializeField]
    [Tooltip("Whether the left obstacle cast should be written to the log file")]
    private bool logLeftHit;

    [SerializeField]
    [Tooltip("Whether the right obstacle cast should be written to the log file")]
    private bool logRightHit;

    [SerializeField]
    [Tooltip("Whether combined obstacle normals should be written to the log file")]
    private bool logCombinedNormal;

    [SerializeField]
    [Tooltip("Whether wall pressure values should be written to the log file")]
    private bool logWallPressure;

    [SerializeField]
    [Tooltip("Whether corner escape state should be written to the log file")]
    private bool logCornerState;

    [SerializeField]
    [Tooltip("Whether stuck time should be written to the log file")]
    private bool logStuckTime;

    [SerializeField]
    [Tooltip("Whether desired avoidance direction should be written to the log file")]
    private bool logDesiredDirection;

    [SerializeField]
    [Tooltip("Whether steering force should be written to the log file")]
    private bool logSteeringForce;

    [SerializeField]
    [Tooltip("Whether only sheep with active data in the enabled log groups should be written to the log file")]
    private bool logOnlyActiveSheep = true;

    private float defaultMoveSpeed;
    private float defaultMaxSteeringForce;
    private float defaultNeighborRadius;
    private float defaultSeparationRadius;
    private float defaultObstacleCheckRadius;
    private float defaultSeparationWeight;
    private float defaultAlignmentWeight;
    private float defaultCohesionWeight;
    private float defaultBoundsWeight;
    private Vector2 panelScrollPosition;
    private Vector2 logPanelScrollPosition;
    private bool wasLoggingEnabled;
    private float logSessionStartTime;
    private float nextLogSampleTime;

    // syncs inspector defaults into the runtime debug state before play begins
    private void Awake()
    {
        ApplyRuntimeSettings();
    }

    // caches the inspector values so each slider can reset back to them during play
    private void Start()
    {
        InitializeLogFile();

        if (flockManager == null)
        {
            flockManager = FindFirstObjectByType<FlockManager>();
        }

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

        if (loggingEnabled)
        {
            BeginLogSession();
        }

        wasLoggingEnabled = loggingEnabled;
    }

    // writes shared flock log samples from the debug panel instead of the flock manager
    private void Update()
    {
        bool hasFenceLogs = HasFenceLogsEnabled();
        bool hasBoidLogs = HasBoidLogsEnabled();
        bool hasWolfLogs = HasWolfLogsEnabled();

        if (!LoggingEnabled || (!hasFenceLogs && !hasBoidLogs && !hasWolfLogs))
        {
            return;
        }

        if (flockManager == null)
        {
            flockManager = FindFirstObjectByType<FlockManager>();
        }

        if (flockManager == null || flockManager.Sheep.Count == 0)
        {
            return;
        }

        float sessionTime = Time.time - logSessionStartTime;

        if (sessionTime < nextLogSampleTime)
        {
            return;
        }

        StringBuilder entry = new StringBuilder();
        entry.AppendLine($"[{nextLogSampleTime:0.000}]");
        int omittedSheepCount = 0;
        int loggedSheepCount = 0;

        for (int i = 0; i < flockManager.Sheep.Count; i++)
        {
            SheepAgent sheep = flockManager.Sheep[i];

            if (sheep == null)
            {
                continue;
            }

            if (LogOnlyActiveSheep && !ShouldLogSheep(sheep, hasFenceLogs, hasBoidLogs))
            {
                omittedSheepCount++;
                continue;
            }

            if (loggedSheepCount > 0)
            {
                entry.AppendLine();
            }

            sheep.AppendDebugLogEntry(entry, "  ");
            loggedSheepCount++;
        }

        if (loggedSheepCount == 0)
        {
            entry.AppendLine("  no sheep matched current log filter");
        }

        if (omittedSheepCount > 0)
        {
            entry.AppendLine();
            entry.AppendLine($"  omitted sheep: {omittedSheepCount}");
        }

        if (hasWolfLogs)
        {
            IReadOnlyList<Wolf> wolves = Wolf.ActiveWolves;
            int loggedWolfCount = 0;

            for (int i = 0; i < wolves.Count; i++)
            {
                Wolf wolf = wolves[i];

                if (wolf == null)
                {
                    continue;
                }

                if (loggedSheepCount > 0 || omittedSheepCount > 0 || loggedWolfCount > 0)
                {
                    entry.AppendLine();
                }

                wolf.AppendDebugLogEntry(entry, "  ");
                loggedWolfCount++;
            }

            if (loggedWolfCount == 0)
            {
                if (loggedSheepCount > 0 || omittedSheepCount > 0)
                {
                    entry.AppendLine();
                }

                entry.AppendLine("  no wolves found");
            }
        }

        AppendLogLine(entry.ToString().TrimEnd());

        while (nextLogSampleTime <= sessionTime)
        {
            nextLogSampleTime += LogInterval;
        }
    }

    // draws the debug panel each frame
    private void OnGUI()
    {
        if (settings == null)
        {
            return;
        }

        Rect panelRect = new Rect(panelPosition.x, panelPosition.y, panelSize.x, panelSize.y);
        GUILayout.BeginArea(panelRect, GUI.skin.box);
        panelScrollPosition = GUILayout.BeginScrollView(panelScrollPosition, false, true);

        GUILayout.BeginHorizontal();
        GUILayout.Label("boid settings");

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
        ShowObstacleCheckRadius = GUILayout.Toggle(ShowObstacleCheckRadius, "show obstacle check range");
        ShowNeighborRadius = GUILayout.Toggle(ShowNeighborRadius, "show neighbor radius");
        ShowLogPanel = GUILayout.Toggle(ShowLogPanel, "show log panel");

        GUILayout.EndScrollView();
        GUILayout.EndArea();

        if (ShowLogPanel)
        {
            DrawLogPanel();
        }

        ApplyRuntimeSettings();

        if (LoggingEnabled && !wasLoggingEnabled)
        {
            BeginLogSession();
        }

        wasLoggingEnabled = LoggingEnabled;
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

    // turns on every fence and boid log category at once
    private void EnableAllLogs()
    {
        logFenceBehavior = true;
        logBoidBehavior = true;
        logWolfBehavior = true;
        logMovementState = true;
        logWolfMovementState = true;
        logNeighborState = true;
        logBoidSummary = true;
        logWolfTargetState = true;
        logWolfRetreatState = true;
        logWolfObstacleState = true;
        logSeparationForce = true;
        logAlignmentForce = true;
        logCohesionForce = true;
        logBoidSteeringForce = true;
        logFrontHit = true;
        logLeftHit = true;
        logRightHit = true;
        logCombinedNormal = true;
        logWallPressure = true;
        logCornerState = true;
        logStuckTime = true;
        logDesiredDirection = true;
        logSteeringForce = true;
        ApplyRuntimeSettings();
    }

    // draws a second panel for enabling and configuring runtime logs
    private void DrawLogPanel()
    {
        Rect logRect = new Rect(
            panelPosition.x + logPanelOffset.x,
            panelPosition.y + logPanelOffset.y,
            logPanelSize.x,
            logPanelSize.y
        );

        GUILayout.BeginArea(logRect, GUI.skin.box);
        logPanelScrollPosition = GUILayout.BeginScrollView(logPanelScrollPosition, false, true);
        GUILayout.Label("log settings");
        GUILayout.Space(8f);

        loggingEnabled = GUILayout.Toggle(loggingEnabled, "enable logging");
        GUILayout.Space(8f);

        GUILayout.Label("writing logs to");
        GUILayout.Label(LogFilePath);

        if (GUILayout.Button("enable all logs"))
        {
            EnableAllLogs();
        }

        GUILayout.Space(8f);

        GUILayout.Label($"log interval: {logInterval:0.00}s");
        logInterval = GUILayout.HorizontalSlider(logInterval, 0.1f, 2f);

        GUILayout.Space(8f);

        logFenceBehavior = GUILayout.Toggle(logFenceBehavior, "log fence behavior");

        if (logFenceBehavior)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            logFrontHit = GUILayout.Toggle(logFrontHit, "log front hit");
            logLeftHit = GUILayout.Toggle(logLeftHit, "log left hit");
            logRightHit = GUILayout.Toggle(logRightHit, "log right hit");
            logCombinedNormal = GUILayout.Toggle(logCombinedNormal, "log combined normal");
            logWallPressure = GUILayout.Toggle(logWallPressure, "log wall pressure");
            logCornerState = GUILayout.Toggle(logCornerState, "log corner state");
            logStuckTime = GUILayout.Toggle(logStuckTime, "log stuck time");
            logDesiredDirection = GUILayout.Toggle(logDesiredDirection, "log desired direction");
            logSteeringForce = GUILayout.Toggle(logSteeringForce, "log steering force");
            GUILayout.EndVertical();
        }

        GUILayout.Space(8f);

        logBoidBehavior = GUILayout.Toggle(logBoidBehavior, "log boid behavior");

        if (logBoidBehavior)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            logMovementState = GUILayout.Toggle(logMovementState, "log movement state");
            logNeighborState = GUILayout.Toggle(logNeighborState, "log neighbor state");
            logBoidSummary = GUILayout.Toggle(logBoidSummary, "log boid summary");
            logSeparationForce = GUILayout.Toggle(logSeparationForce, "log separation force");
            logAlignmentForce = GUILayout.Toggle(logAlignmentForce, "log alignment force");
            logCohesionForce = GUILayout.Toggle(logCohesionForce, "log cohesion force");
            logBoidSteeringForce = GUILayout.Toggle(logBoidSteeringForce, "log boid steering force");
            GUILayout.EndVertical();
        }

        GUILayout.Space(8f);

        logWolfBehavior = GUILayout.Toggle(logWolfBehavior, "log wolf behavior");

        if (logWolfBehavior)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            logWolfMovementState = GUILayout.Toggle(logWolfMovementState, "log wolf movement state");
            logWolfTargetState = GUILayout.Toggle(logWolfTargetState, "log wolf target state");
            logWolfRetreatState = GUILayout.Toggle(logWolfRetreatState, "log wolf retreat state");
            logWolfObstacleState = GUILayout.Toggle(logWolfObstacleState, "log wolf obstacle state");
            GUILayout.EndVertical();
        }

        GUILayout.Space(8f);

        logOnlyActiveSheep = GUILayout.Toggle(logOnlyActiveSheep, "only log active sheep");

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    // sets up the plain text log file used by the debug log panel
    private void InitializeLogFile()
    {
        string logsDirectory = Path.Combine(Application.dataPath, "Logs");
        Directory.CreateDirectory(logsDirectory);
        LogFilePath = Path.Combine(logsDirectory, "boid-debug-log.txt");
        ResetLogFile();
    }

    // applies inspector log values to the shared runtime debug state
    private void ApplyRuntimeSettings()
    {
        LoggingEnabled = loggingEnabled;
        LogInterval = Mathf.Max(0.1f, logInterval);
        LogFenceBehavior = logFenceBehavior;
        LogBoidBehavior = logBoidBehavior;
        LogWolfBehavior = logWolfBehavior;
        LogMovementState = logMovementState;
        LogWolfMovementState = logWolfMovementState;
        LogNeighborState = logNeighborState;
        LogBoidSummary = logBoidSummary;
        LogWolfTargetState = logWolfTargetState;
        LogWolfRetreatState = logWolfRetreatState;
        LogWolfObstacleState = logWolfObstacleState;
        LogSeparationForce = logSeparationForce;
        LogAlignmentForce = logAlignmentForce;
        LogCohesionForce = logCohesionForce;
        LogBoidSteeringForce = logBoidSteeringForce;
        LogFrontHit = logFrontHit;
        LogLeftHit = logLeftHit;
        LogRightHit = logRightHit;
        LogCombinedNormal = logCombinedNormal;
        LogWallPressure = logWallPressure;
        LogCornerState = logCornerState;
        LogStuckTime = logStuckTime;
        LogDesiredDirection = logDesiredDirection;
        LogSteeringForce = logSteeringForce;
        LogOnlyActiveSheep = logOnlyActiveSheep;
    }

    // starts a fresh log session using the current panel settings
    private void BeginLogSession()
    {
        ResetLogFile();
        logSessionStartTime = Time.time;
        nextLogSampleTime = LogInterval;
    }

    // clears the log file and writes a small session header
    private void ResetLogFile()
    {
        if (string.IsNullOrEmpty(LogFilePath))
        {
            return;
        }

        File.WriteAllText(LogFilePath, BuildLogHeader());
    }

    // builds the header block for the current logging session
    private string BuildLogHeader()
    {
        return
            $"boid debug log {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
            $"move speed = {settings.MoveSpeed:0.00}\n" +
            $"max steering force = {settings.MaxSteeringForce:0.00}\n" +
            $"neighbor radius = {settings.NeighborRadius:0.00}\n" +
            $"separation radius = {settings.SeparationRadius:0.00}\n" +
            $"obstacle check radius = {settings.ObstacleCheckRadius:0.00}\n" +
            $"separation weight = {settings.SeparationWeight:0.00}\n" +
            $"alignment weight = {settings.AlignmentWeight:0.00}\n" +
            $"cohesion weight = {settings.CohesionWeight:0.00}\n" +
            $"bounds weight = {settings.BoundsWeight:0.00}\n" +
            $"log interval = {LogInterval:0.00}\n" +
            $"log fence behavior = {LogFenceBehavior}\n" +
            $"log boid behavior = {LogBoidBehavior}\n" +
            $"log wolf behavior = {LogWolfBehavior}\n" +
            $"log movement state = {LogMovementState}\n" +
            $"log wolf movement state = {LogWolfMovementState}\n" +
            $"log neighbor state = {LogNeighborState}\n" +
            $"log boid summary = {LogBoidSummary}\n" +
            $"log wolf target state = {LogWolfTargetState}\n" +
            $"log wolf retreat state = {LogWolfRetreatState}\n" +
            $"log wolf obstacle state = {LogWolfObstacleState}\n" +
            $"log separation force = {LogSeparationForce}\n" +
            $"log alignment force = {LogAlignmentForce}\n" +
            $"log cohesion force = {LogCohesionForce}\n" +
            $"log boid steering force = {LogBoidSteeringForce}\n" +
            $"log front hit = {LogFrontHit}\n" +
            $"log left hit = {LogLeftHit}\n" +
            $"log right hit = {LogRightHit}\n" +
            $"log combined normal = {LogCombinedNormal}\n" +
            $"log wall pressure = {LogWallPressure}\n" +
            $"log corner state = {LogCornerState}\n" +
            $"log stuck time = {LogStuckTime}\n" +
            $"log desired direction = {LogDesiredDirection}\n" +
            $"log steering force = {LogSteeringForce}\n" +
            $"only log active sheep = {LogOnlyActiveSheep}\n\n";
    }

    // checks whether any fence log category is enabled
    private bool HasFenceLogsEnabled()
    {
        return LogFenceBehavior
            && (LogFrontHit
                || LogLeftHit
                || LogRightHit
                || LogCombinedNormal
                || LogWallPressure
                || LogCornerState
                || LogStuckTime
                || LogDesiredDirection
                || LogSteeringForce);
    }

    // checks whether any boid log category is enabled
    private bool HasBoidLogsEnabled()
    {
        return LogBoidBehavior
            && (LogMovementState
                || LogNeighborState
                || LogBoidSummary
                || LogSeparationForce
                || LogAlignmentForce
                || LogCohesionForce
                || LogBoidSteeringForce);
    }

    // checks whether any wolf log category is enabled
    private bool HasWolfLogsEnabled()
    {
        return LogWolfBehavior
            && (LogWolfMovementState
                || LogWolfTargetState
                || LogWolfRetreatState
                || LogWolfObstacleState);
    }

    // checks whether this sheep should be included in the current filtered log sample
    private bool ShouldLogSheep(SheepAgent sheep, bool hasFenceLogs, bool hasBoidLogs)
    {
        return (hasFenceLogs && sheep.HasActiveObstacleDebugState())
            || (hasBoidLogs && sheep.HasActiveBoidDebugState());
    }

    // appends one plain text log entry to the shared debug log file
    public static void AppendLogLine(string line)
    {
        if (string.IsNullOrEmpty(LogFilePath))
        {
            return;
        }

        File.AppendAllText(LogFilePath, line + "\n\n");
    }

    // draws the obstacle check range around fence colliders
    private void OnDrawGizmos()
    {
        if (!ShowObstacleCheckRadius || settings == null)
        {
            return;
        }

        int obstacleLayer = LayerMask.NameToLayer("Obstacle");

        if (obstacleLayer < 0)
        {
            return;
        }

        Collider2D[] colliders = FindObjectsByType<Collider2D>(FindObjectsSortMode.None);
        Gizmos.color = new Color(1f, 0.75f, 0.2f, 1f);

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D collider = colliders[i];

            if (collider == null || !collider.enabled || collider.gameObject.layer != obstacleLayer)
            {
                continue;
            }

            Bounds bounds = collider.bounds;
            bounds.Expand(settings.ObstacleCheckRadius * 2f);
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }
}
