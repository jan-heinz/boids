using UnityEngine;

// draws a small in game panel for tuning boid settings during play
public class BoidDebugPanel : MonoBehaviour
{
    public static bool ShowObstacleCheckRadius { get; private set; }
    public static bool ShowNeighborRadius { get; private set; }

    // -------------------------------------------------------------------------------------------------------------

    [Header("References")]
    [SerializeField]
    [Tooltip("Boid settings asset the panel should edit")]
    private BoidSettings settings;

    // -------------------------------------------------------------------------------------------------------------

    [Header("Layout")]
    [SerializeField]
    [Tooltip("Screen position of the panel from the top left corner")]
    private Vector2 panelPosition = new Vector2(16f, 16f);

    [SerializeField]
    [Tooltip("Width and height of the panel")]
    private Vector2 panelSize = new Vector2(320f, 420f);

    private float defaultMoveSpeed;
    private float defaultMaxSteeringForce;
    private float defaultNeighborRadius;
    private float defaultSeparationRadius;
    private float defaultObstacleCheckRadius;
    private float defaultSeparationWeight;
    private float defaultAlignmentWeight;
    private float defaultCohesionWeight;
    private float defaultBoundsWeight;

    // caches the inspector values so each slider can reset back to them during play
    private void Start()
    {
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
