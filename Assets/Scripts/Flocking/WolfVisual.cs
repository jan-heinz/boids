using UnityEngine;

// swaps simple wolf sprites from the current movement direction and speed
public class WolfVisual : MonoBehaviour
{
    private enum FacingDirection
    {
        Right,
        Left
    }

    [Header("References")]
    [SerializeField]
    [Tooltip("Wolf logic this visual should read from")]
    private Wolf wolf;

    [SerializeField]
    [Tooltip("Renderer that should display the wolf sprite")]
    private SpriteRenderer spriteRenderer;

    [Header("Run Sprites")]
    [SerializeField]
    [Tooltip("Run frames facing right")]
    private Sprite[] runRight;

    [SerializeField]
    [Tooltip("Run frames facing left")]
    private Sprite[] runLeft;

    [Header("Animation")]
    [SerializeField, Min(0f)]
    [Tooltip("How fast the wolf must move before the run animation starts")]
    private float runSpeedThreshold = 0.1f;

    [SerializeField, Min(0f)]
    [Tooltip("How quickly the run animation flips between frames")]
    private float runFrameRate = 8f;

    [SerializeField, Min(0f)]
    [Tooltip("How much horizontal movement is needed before the wolf flips facing")]
    private float facingSwitchThreshold = 0.2f;

    [SerializeField, Min(0f)]
    [Tooltip("How much stronger horizontal movement must be than vertical movement before the wolf flips facing")]
    private float horizontalDominanceThreshold = 0.05f;

    private FacingDirection facing = FacingDirection.Right;
    private float runTimer;
    private int currentRunFrame;

    // finds the common references if they were not set in the inspector
    private void Awake()
    {
        if (wolf == null)
        {
            wolf = GetComponentInParent<Wolf>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    // updates the displayed sprite from the wolf state and movement
    private void Update()
    {
        if (wolf == null || spriteRenderer == null)
        {
            return;
        }

        Vector2 velocity = wolf.CurrentVelocity;
        float speed = velocity.magnitude;

        if (speed > 0.01f)
        {
            facing = GetFacingDirection(velocity);
        }

        if (speed <= runSpeedThreshold)
        {
            runTimer = 0f;
            currentRunFrame = 0;
            spriteRenderer.sprite = GetRunFrame(facing, 0);
            return;
        }

        Sprite[] runFrames = GetRunFrames(facing);

        if (runFrames == null || runFrames.Length == 0)
        {
            spriteRenderer.sprite = GetFallbackFrame();
            return;
        }

        runTimer += Time.deltaTime * runFrameRate;

        if (runTimer >= 1f)
        {
            runTimer = 0f;
            currentRunFrame = (currentRunFrame + 1) % runFrames.Length;
        }

        spriteRenderer.sprite = runFrames[currentRunFrame];
    }

    // chooses the main direction from the current movement vector
    private FacingDirection GetFacingDirection(Vector2 velocity)
    {
        float horizontalSpeed = Mathf.Abs(velocity.x);
        float verticalSpeed = Mathf.Abs(velocity.y);

        if (horizontalSpeed >= facingSwitchThreshold &&
            horizontalSpeed >= verticalSpeed + horizontalDominanceThreshold)
        {
            return velocity.x >= 0f ? FacingDirection.Right : FacingDirection.Left;
        }

        return facing;
    }

    // gets the run frames for the current facing direction
    private Sprite[] GetRunFrames(FacingDirection direction)
    {
        return direction == FacingDirection.Right ? runRight : runLeft;
    }

    // gets a stable frame from the requested direction if it exists
    private Sprite GetRunFrame(FacingDirection direction, int frameIndex)
    {
        Sprite[] runFrames = GetRunFrames(direction);

        if (runFrames == null || runFrames.Length == 0)
        {
            return GetFallbackFrame();
        }

        frameIndex = Mathf.Clamp(frameIndex, 0, runFrames.Length - 1);
        return runFrames[frameIndex];
    }

    // falls back to the first available run frame so the wolf still renders
    private Sprite GetFallbackFrame()
    {
        if (runRight != null && runRight.Length > 0)
        {
            return runRight[0];
        }

        if (runLeft != null && runLeft.Length > 0)
        {
            return runLeft[0];
        }

        return null;
    }
}
