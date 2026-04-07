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

    // -------------------------------------------------------------------------------------------------------------

    [Header("Spawning")]
    [SerializeField, Min(1)]
    [Tooltip("How many sheep to spawn when play mode begins")]
    private int sheepCount = 10;

    [SerializeField]
    [Tooltip("Whether each sheep should get a random starting direction")]
    private bool randomizeStartingDirection = true;

    private Bounds spawnBounds;
    private readonly List<SheepAgent> sheep = new();

    public IReadOnlyList<SheepAgent> Sheep => sheep;

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
            sheep.SetFlockManager(this);
            this.sheep.Add(sheep);

            if (!randomizeStartingDirection)
            {
                continue;
            }

            sheep.SetStartingDirection(GetRandomDirection());
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
