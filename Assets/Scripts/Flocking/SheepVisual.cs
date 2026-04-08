using UnityEngine;

// swaps simple sheep sprites based on movement direction and speed
public class SheepVisual : MonoBehaviour
{
    // -------------------------------------------------------------------------------------------------------------

    [Header("References")]
    [SerializeField]
    [Tooltip("Sheep movement script this visual should read from")]
    private SheepAgent sheepAgent;

    [SerializeField]
    [Tooltip("Renderer that should display the sheep sprite")]
    private SpriteRenderer spriteRenderer;

    [Header("Sprites")]
    [SerializeField]
    [Tooltip("Idle sprite for the sheep")]
    private Sprite idleSprite;

    [SerializeField]
    [Tooltip("Walk frames used while the sheep is moving")]
    private Sprite[] walkFrames;

    [SerializeField]
    [Tooltip("Whether the source sheep art is facing right by default")]
    private bool sourceFacesRight = true;

    [Header("Animation")]
    [SerializeField, Min(0f)]
    [Tooltip("How fast the sheep must move before the walk animation starts")]
    private float walkSpeedThreshold = 0.1f;

    [SerializeField, Min(0f)]
    [Tooltip("How quickly the simple walk animation flips between frames")]
    private float walkFrameRate = 6f;

    private float walkTimer;
    private int currentWalkFrame;
    private bool facingRight = true;

    // finds the common references if they were not set in the inspector
    private void Awake()
    {
        if (sheepAgent == null)
        {
            sheepAgent = GetComponentInParent<SheepAgent>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }

    // updates the displayed sprite from the current movement direction
    private void Update()
    {
        if (sheepAgent == null || spriteRenderer == null)
        {
            return;
        }

        Vector2 velocity = sheepAgent.CurrentVelocity;
        float speed = velocity.magnitude;

        if (Mathf.Abs(velocity.x) > 0.01f)
        {
            facingRight = velocity.x > 0f;
        }

        if (speed <= walkSpeedThreshold)
        {
            walkTimer = 0f;
            currentWalkFrame = 0;
            spriteRenderer.sprite = idleSprite;
            spriteRenderer.flipX = facingRight != sourceFacesRight;
            return;
        }

        walkTimer += Time.deltaTime * walkFrameRate;

        if (walkFrames == null || walkFrames.Length == 0)
        {
            spriteRenderer.sprite = idleSprite;
            spriteRenderer.flipX = facingRight != sourceFacesRight;
            return;
        }

        if (walkTimer >= 1f)
        {
            walkTimer = 0f;
            currentWalkFrame = (currentWalkFrame + 1) % walkFrames.Length;
        }

        spriteRenderer.sprite = walkFrames[currentWalkFrame];
        spriteRenderer.flipX = facingRight != sourceFacesRight;
    }
}
