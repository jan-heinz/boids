using UnityEngine;
using System.Collections.Generic;

// spawns a group of sheep into the scene without any flock behavior yet
public class FlockManager : MonoBehaviour
{
    // -------------------------------------------------------------------------------------------------------------

    [Header("References")]
    [SerializeField]
    [Tooltip("Sheep prefab the manager should spawn")]
    private SheepAgent sheepPrefab;

    [SerializeField]
    [Tooltip("Optional parent for spawned sheep in the hierarchy")]
    private Transform sheepParent;

    [SerializeField]
    [Tooltip("Box collider that defines where sheep can spawn")]
    private BoxCollider2D spawnArea;

    [SerializeField]
    [Tooltip("Optional box collider that marks the area sheep should start moving toward")]
    private BoxCollider2D goalArea;

    // -------------------------------------------------------------------------------------------------------------

    [Header("Spawning")]
    [SerializeField, Min(1)]
    [Tooltip("How many sheep to spawn when play mode begins")]
    private int sheepCount = 10;

    [SerializeField]
    [Tooltip("Whether sheep should get a random starting direction when no goal area is assigned")]
    private bool randomizeStartingDirection = true;

    [SerializeField, Range(0f, 90f)]
    [Tooltip("How much random spread to allow around the goal direction in degrees")]
    private float startingDirectionSpread = 30f;

    private Bounds spawnBounds;
    private readonly List<SheepAgent> sheep = new();

    public IReadOnlyList<SheepAgent> Sheep => sheep;
    public int SheepCount => sheepCount;

    // spawns the initial sheep group when play mode begins
    private void Start()
    {
        if (sheepPrefab == null)
        {
            Debug.LogError("FlockManager needs a SheepAgent prefab assigned", this);
            enabled = false;
            return;
        }

        if (spawnArea == null)
        {
            Debug.LogError("FlockManager needs a BoxCollider2D spawn area assigned", this);
            enabled = false;
            return;
        }

        spawnBounds = spawnArea.bounds;

        // disables the spawn area collider so sheep do not collide with it like a wall
        spawnArea.enabled = false;

        SpawnSheep();
    }

    // creates the requested number of sheep inside the spawn area
    private void SpawnSheep()
    {
        for (int i = 0; i < sheepCount; i++)
        {
            Vector2 spawnPosition = GetRandomSpawnPosition();
            SheepAgent sheep = Instantiate(sheepPrefab, spawnPosition, Quaternion.identity, sheepParent);
            sheep.name = $"Sheep {i + 1}";
            sheep.SetFlockManager(this);
            this.sheep.Add(sheep);

            sheep.SetStartingDirection(GetStartingDirection(spawnPosition));
        }
    }

    // picks a random point inside the spawn area's world bounds
    private Vector2 GetRandomSpawnPosition()
    {
        return new Vector2(
            Random.Range(spawnBounds.min.x, spawnBounds.max.x),
            Random.Range(spawnBounds.min.y, spawnBounds.max.y)
        );
    }

    // picks the initial heading for a spawned sheep
    private Vector2 GetStartingDirection(Vector2 spawnPosition)
    {
        if (goalArea != null)
        {
            return GetGoalBiasedDirection(spawnPosition);
        }

        if (randomizeStartingDirection)
        {
            return GetRandomDirection();
        }

        return Vector2.right;
    }

    // points a spawned sheep toward the goal area with a little spread
    private Vector2 GetGoalBiasedDirection(Vector2 spawnPosition)
    {
        Vector2 goalDirection = (Vector2)goalArea.bounds.center - spawnPosition;

        if (goalDirection.sqrMagnitude <= 0.0001f)
        {
            goalDirection = Vector2.right;
        }

        float angleOffset = Random.Range(-startingDirectionSpread, startingDirectionSpread);
        return Quaternion.Euler(0f, 0f, angleOffset) * goalDirection.normalized;
    }

    // picks a random 2d direction for a spawned sheep
    private Vector2 GetRandomDirection()
    {
        Vector2 direction = Random.insideUnitCircle;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            return Vector2.right;
        }

        return direction.normalized;
    }
}
