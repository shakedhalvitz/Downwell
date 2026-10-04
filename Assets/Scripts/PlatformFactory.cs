using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public enum SpawnEnvironment
{
    RequireFloor,
    RequireAir
}

[System.Serializable]
public class ObjectSpawnData
{
    public string objectName;
    public GameObject prefab;
    public SpawnEnvironment environment;
    [Range(0f, 1f)] public float spawnChance = 0.3f;
    public int poolCapacity = 15;
}

public class PlatformFactory : MonoBehaviour
{
    public static PlatformFactory Instance { get; private set; }

    [Header("Prefabs")]
    public GameObject solidPlatformPrefab;
    public GameObject breakablePlatformPrefab;

    [Header("Spawnable Objects (Enemies, Coins)")]
    public List<ObjectSpawnData> spawnableObjects = new List<ObjectSpawnData>();

    [Header("Grid Settings")]
    public float tileSize = 0.2f;
    public float safeBoundX = 2.5f;

    [Header("Solid Platforms")]
    public float minSolidSpacing = 3.0f;
    public float maxSolidSpacing = 6.0f;
    public int minSolidHoleSize = 4;
    public int maxSolidHoleSize = 12;

    [Header("Breakable Boxes")]
    [Range(0f, 1f)] public float breakableBaseChance = 0.02f;
    [Range(0f, 1f)] public float breakableClusterChance = 0.65f;
    public int gapUnderSolid = 2;

    private ObjectPool<GameObject> solidPool;
    private ObjectPool<GameObject> breakablePool;

    private Dictionary<string, ObjectPool<GameObject>> objectPools = new Dictionary<string, ObjectPool<GameObject>>();

    // Every object currently taken out of a pool, and the pool it must go back to
    private Dictionary<GameObject, ObjectPool<GameObject>> activeObjects = new Dictionary<GameObject, ObjectPool<GameObject>>();
    private readonly List<GameObject> releaseBuffer = new List<GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        solidPool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(solidPlatformPrefab),
            actionOnGet: obj => obj.SetActive(true),
            actionOnRelease: obj => obj.SetActive(false),
            actionOnDestroy: Destroy,
            defaultCapacity: 100
        );

        breakablePool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(breakablePlatformPrefab),
            actionOnGet: obj => obj.SetActive(true),
            actionOnRelease: obj => obj.SetActive(false),
            actionOnDestroy: Destroy,
            defaultCapacity: 150
        );

        foreach (var objData in spawnableObjects)
        {
            if (objData.prefab != null && !objectPools.ContainsKey(objData.objectName))
            {
                objectPools.Add(objData.objectName, new ObjectPool<GameObject>(
                    createFunc: () => Instantiate(objData.prefab),
                    actionOnGet: obj => obj.SetActive(true),
                    actionOnRelease: obj => obj.SetActive(false),
                    actionOnDestroy: Destroy,
                    defaultCapacity: objData.poolCapacity
                ));
            }
        }
    }

    public void GeneratePlatformsInArea(float topY, float bottomY)
    {
        float height = Mathf.Abs(topY - bottomY);
        int rows = Mathf.CeilToInt(height / tileSize);
        int cols = Mathf.FloorToInt((safeBoundX * 2f) / tileSize);
        float startX = -safeBoundX + (tileSize / 2f);

        int[,] grid = new int[rows, cols];

        // 1. Solid Platforms & Floor Objects (Slimes)
        int r = Mathf.RoundToInt(UnityEngine.Random.Range(minSolidSpacing, maxSolidSpacing) / tileSize);
        while (r < rows)
        {
            int holeSize = UnityEngine.Random.Range(minSolidHoleSize, maxSolidHoleSize + 1);
            int holeStart = UnityEngine.Random.Range(0, cols - holeSize + 1);

            int consecutiveSolidCount = 0;
            float currentPlatformY = topY - (r * tileSize);

            for (int c = 0; c < cols; c++)
            {
                if (c < holeStart || c >= holeStart + holeSize)
                {
                    grid[r, c] = 1;
                    SpawnBlock(solidPool, startX + (c * tileSize), currentPlatformY);

                    consecutiveSolidCount++;

                    if (consecutiveSolidCount == 5)
                    {
                        float spawnX = startX + ((c - 2) * tileSize);
                        float spawnY = currentPlatformY + tileSize;
                        TrySpawnObject(SpawnEnvironment.RequireFloor, spawnX, spawnY);
                        consecutiveSolidCount = 0;
                    }
                }
                else
                {
                    consecutiveSolidCount = 0;
                }
            }
            r += Mathf.RoundToInt(UnityEngine.Random.Range(minSolidSpacing, maxSolidSpacing) / tileSize);
        }

        // 2. Breakable Boxes
        for (int row = 0; row < rows; row++)
        {
            for (int col = 1; col < cols - 1; col++)
            {
                if (grid[row, col] != 0) continue;

                bool hasSolidAbove = false;
                for (int i = 1; i <= gapUnderSolid; i++)
                {
                    if (row - i >= 0 && grid[row - i, col] == 1)
                    {
                        hasSolidAbove = true;
                        break;
                    }
                }
                if (hasSolidAbove) continue;

                bool hasNeighbor = false;
                for (int dr = -1; dr <= 1; dr++)
                {
                    for (int dc = -1; dc <= 1; dc++)
                    {
                        if (dr == 0 && dc == 0) continue;
                        int nr = row + dr;
                        int nc = col + dc;

                        if (nr >= 0 && nr < rows && nc >= 0 && nc < cols)
                        {
                            if (grid[nr, nc] == 2) hasNeighbor = true;
                        }
                    }
                }

                float chance = hasNeighbor ? breakableClusterChance : breakableBaseChance;

                if (UnityEngine.Random.value <= chance)
                {
                    grid[row, col] = 2;
                    SpawnBlock(breakablePool, startX + (col * tileSize), topY - (row * tileSize));
                }
            }
        }

        // 3. Air Objects (Bats)
        // Check empty spaces and try to spawn flying objects
        for (int row = 0; row < rows; row += 2) // Jump every 2 rows to avoid flooding the screen
        {
            // Pick a random column away from the edges
            int randomCol = UnityEngine.Random.Range(2, cols - 2);

            if (grid[row, randomCol] == 0)
            {
                float spawnX = startX + (randomCol * tileSize);
                float spawnY = topY - (row * tileSize);
                TrySpawnObject(SpawnEnvironment.RequireAir, spawnX, spawnY);
            }
        }
    }

    private void SpawnBlock(ObjectPool<GameObject> pool, float x, float y)
    {
        ContactFilter2D filter = new ContactFilter2D();
        filter.useTriggers = true;

        List<Collider2D> hits = new List<Collider2D>();
        Physics2D.OverlapBox(new Vector2(x, y), new Vector2(tileSize * 0.9f, tileSize * 0.9f), 0f, filter, hits);

        foreach (var hit in hits)
        {
            if (hit.isTrigger && (hit.CompareTag("Platform") || hit.CompareTag("BreakablePlatform")))
            {
                return;
            }
        }

        GetFromPool(pool, new Vector2(x, y));
    }

    private GameObject GetFromPool(ObjectPool<GameObject> pool, Vector2 position)
    {
        GameObject obj = pool.Get();
        obj.transform.position = position;
        activeObjects[obj] = pool;
        return obj;
    }

    /// <summary>
    /// Returns a spawned object (platform, box, enemy, collectable) to its pool.
    /// Safe to call more than once - only the first call releases it.
    /// </summary>
    public void Release(GameObject obj)
    {
        if (obj != null && activeObjects.TryGetValue(obj, out ObjectPool<GameObject> pool))
        {
            activeObjects.Remove(obj);
            pool.Release(obj);
        }
    }

    /// <summary>
    /// Returns every spawned object above the given height to its pool (called when old segments scroll away).
    /// </summary>
    public void ReleaseObjectsAbove(float y)
    {
        releaseBuffer.Clear();
        foreach (GameObject obj in activeObjects.Keys)
        {
            if (obj.transform.position.y > y)
            {
                releaseBuffer.Add(obj);
            }
        }

        foreach (GameObject obj in releaseBuffer)
        {
            Release(obj);
        }
    }

    private void TrySpawnObject(SpawnEnvironment requiredEnv, float spawnX, float spawnY)
    {
        foreach (var objData in spawnableObjects)
        {
            if (objData.environment == requiredEnv && UnityEngine.Random.value <= objData.spawnChance)
            {
                if (objectPools.TryGetValue(objData.objectName, out ObjectPool<GameObject> pool))
                {
                    GameObject newObj = GetFromPool(pool, new Vector2(spawnX, spawnY));

                    // All releases go through Release() so an object is never returned twice
                    SlimeMonster slime = newObj.GetComponent<SlimeMonster>();
                    if (slime != null)
                    {
                        slime.Setup(Release);
                    }

                    BatMonster bat = newObj.GetComponent<BatMonster>();
                    if (bat != null)
                    {
                        bat.Setup(Release);
                    }

                    CollectableObject collectable = newObj.GetComponent<CollectableObject>();
                    if (collectable != null)
                    {
                        collectable.Setup(Release);
                    }

                    return;
                }
            }
        }
    }
}