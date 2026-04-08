using UnityEngine;

// swaps simple dog sprites from the current movement direction and speed
public class SheepdogVisual : MonoBehaviour
{
    private enum FacingDirection
    {
        Up,
        Right,
        Left,
        Down
    }

    // -------------------------------------------------------------------------------------------------------------

    [Header("References")]
    [SerializeField]
    [Tooltip("Sheepdog movement script this visual should read from")]
    private Sheepdog sheepdog;

    [SerializeField]
    [Tooltip("Renderer that should display the dog sprite")]
    private SpriteRenderer spriteRenderer;

    [Header("Idle Sprites")]
    [SerializeField]
    [Tooltip("Idle sprite facing up")]
    private Sprite idleUp;

    [SerializeField]
    [Tooltip("Idle sprite facing right")]
    private Sprite idleRight;

    [SerializeField]
    [Tooltip("Idle sprite facing left")]
    private Sprite idleLeft;

    [SerializeField]
    [Tooltip("Idle sprite facing down")]
    private Sprite idleDown;

    [Header("Walk Sprites")]
    [SerializeField]
    [Tooltip("Walk frames facing up")]
    private Sprite[] walkUp;

    [SerializeField]
    [Tooltip("Walk frames facing right")]
    private Sprite[] walkRight;

    [SerializeField]
    [Tooltip("Walk frames facing left")]
    private Sprite[] walkLeft;

    [SerializeField]
    [Tooltip("Walk frames facing down")]
    private Sprite[] walkDown;

    [Header("Animation")]
    [SerializeField, Min(0f)]
    [Tooltip("How fast the dog must move before the walk animation starts")]
    private float walkSpeedThreshold = 0.1f;

    [SerializeField, Min(0f)]
    [Tooltip("How quickly the walk animation flips between frames")]
    private float walkFrameRate = 8f;

    private FacingDirection facing = FacingDirection.Down;
    private float walkTimer;
    private int currentWalkFrame;

    // finds the common references if they were not set in the inspector
    private void Awake()
    {
        if (sheepdog == null)
        {
            sheepdog = GetComponentInParent<Sheepdog>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    // updates the displayed sprite from the current dog velocity
    private void Update()
    {
        if (sheepdog == null || spriteRenderer == null)
        {
            return;
        }

        Vector2 velocity = sheepdog.CurrentVelocity;
        float speed = velocity.magnitude;

        if (speed > 0.01f)
        {
            facing = GetFacingDirection(velocity);
        }

        if (speed <= walkSpeedThreshold)
        {
            walkTimer = 0f;
            currentWalkFrame = 0;
            spriteRenderer.sprite = GetIdleSprite(facing);
            return;
        }

        Sprite[] walkFrames = GetWalkFrames(facing);

        if (walkFrames == null || walkFrames.Length == 0)
        {
            spriteRenderer.sprite = GetIdleSprite(facing);
            return;
        }

        walkTimer += Time.deltaTime * walkFrameRate;

        if (walkTimer >= 1f)
        {
            walkTimer = 0f;
            currentWalkFrame = (currentWalkFrame + 1) % walkFrames.Length;
        }

        spriteRenderer.sprite = walkFrames[currentWalkFrame];
    }

    // chooses the main direction from the current movement vector
    private FacingDirection GetFacingDirection(Vector2 velocity)
    {
        if (Mathf.Abs(velocity.x) > Mathf.Abs(velocity.y))
        {
            return velocity.x >= 0f ? FacingDirection.Right : FacingDirection.Left;
        }

        return velocity.y >= 0f ? FacingDirection.Up : FacingDirection.Down;
    }

    // gets the idle sprite for the current facing direction
    private Sprite GetIdleSprite(FacingDirection direction)
    {
        return direction switch
        {
            FacingDirection.Up => idleUp,
            FacingDirection.Right => idleRight,
            FacingDirection.Left => idleLeft,
            _ => idleDown
        };
    }

    // gets the walk frames for the current facing direction
    private Sprite[] GetWalkFrames(FacingDirection direction)
    {
        return direction switch
        {
            FacingDirection.Up => walkUp,
            FacingDirection.Right => walkRight,
            FacingDirection.Left => walkLeft,
            _ => walkDown
        };
    }
}
