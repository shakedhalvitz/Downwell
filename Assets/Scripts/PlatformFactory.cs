using UnityEngine;
using UnityEngine.Pool;
using System.Collections.Generic;

public class PlatformFactory : MonoBehaviour
{
    public static PlatformFactory Instance { get; private set; }

    [Header("Prefabs")]
    public GameObject solidPlatformPrefab;
    public GameObject breakablePlatformPrefab;

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

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
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
    }

    public void GeneratePlatformsInArea(float topY, float bottomY)
    {
        float height = Mathf.Abs(topY - bottomY);
        int rows = Mathf.CeilToInt(height / tileSize);
        int cols = Mathf.FloorToInt((safeBoundX * 2f) / tileSize);
        float startX = -safeBoundX + (tileSize / 2f);

        int[,] grid = new int[rows, cols];

        int r = Mathf.RoundToInt(Random.Range(minSolidSpacing, maxSolidSpacing) / tileSize);
        while (r < rows)
        {
            int holeSize = Random.Range(minSolidHoleSize, maxSolidHoleSize + 1);
            int holeStart = Random.Range(0, cols - holeSize + 1);

            for (int c = 0; c < cols; c++)
            {
                if (c < holeStart || c >= holeStart + holeSize)
                {
                    grid[r, c] = 1;
                    SpawnBlock(solidPool, startX + (c * tileSize), topY - (r * tileSize));
                }
            }
            r += Mathf.RoundToInt(Random.Range(minSolidSpacing, maxSolidSpacing) / tileSize);
        }

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

                if (Random.value <= chance)
                {
                    grid[row, col] = 2;
                    SpawnBlock(breakablePool, startX + (col * tileSize), topY - (row * tileSize));
                }
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

        GameObject block = pool.Get();
        block.transform.position = new Vector2(x, y);
    }
}