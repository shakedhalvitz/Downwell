using System.Collections.Generic;
using UnityEngine;

public class LevelGenerator : Singleton<LevelGenerator>
{
    [Header("References")]
    [SerializeField] private Transform player; // Drag the Player object here in the Inspector

    [Header("Prefabs")]
    [SerializeField] private GameObject wallsPrefab;

    [Header("Settings")]
    [SerializeField] private float segmentHeight = 20f;
    [SerializeField] private int initialSegments = 3;
    [SerializeField] private float spawnDistanceThreshold = 40f; // Distance below player to trigger spawning

    private float currentSpawnY = 3f;
    private List<GameObject> activeWalls = new List<GameObject>();

    // Areas whose platforms/enemies wait for the game to start (walls are visible behind the start menu, the rest isn't)
    private readonly List<Vector2> pendingAreas = new List<Vector2>();

    void Start()
    {
        GameManager.Instance.OnGameStarted += GeneratePendingAreas;

        for (int i = 0; i < initialSegments; i++)
        {
            SpawnNextSegment();
        }

        // GameManager.Start may have already started the game (e.g. after a restart)
        if (GameManager.Instance.IsPlaying)
        {
            GeneratePendingAreas();
        }
    }

    protected override void OnDestroy()
    {
        if (GameManager.HasInstance)
        {
            GameManager.Instance.OnGameStarted -= GeneratePendingAreas;
        }
        base.OnDestroy();
    }

    private void GeneratePendingAreas()
    {
        foreach (Vector2 area in pendingAreas)
        {
            GenerateArea(area.x, area.y);
        }
        pendingAreas.Clear();
    }

    private void GenerateArea(float topY, float bottomY)
    {
        if (PlatformFactory.Instance != null)
        {
            PlatformFactory.Instance.GeneratePlatformsInArea(topY, bottomY);
        }
    }

    void Update()
    {
        // Distance-based generation guarantees we never miss a trigger zone[cite: 6]
        if (player != null)
        {
            if (player.position.y - spawnDistanceThreshold < currentSpawnY)
            {
                SpawnNextSegment();
            }
        }
    }

    public void SpawnNextSegment()
    {
        Vector3 spawnPosition = new Vector3(7.18f, currentSpawnY, 0f);
        GameObject newWalls = Instantiate(wallsPrefab, spawnPosition, Quaternion.identity);
        activeWalls.Add(newWalls);

        float topY = currentSpawnY;
        float bottomY = currentSpawnY - segmentHeight;

        if (GameManager.Instance.IsPlaying)
        {
            GenerateArea(topY, bottomY);
        }
        else
        {
            pendingAreas.Add(new Vector2(topY, bottomY));
        }

        currentSpawnY -= segmentHeight;
        CleanupOldSegments();
    }

    void CleanupOldSegments()
    {
        // Increased the buffer to 5 segments to prevent deleting walls visible on screen
        if (activeWalls.Count > 5)
        {
            GameObject oldWalls = activeWalls[0];
            activeWalls.RemoveAt(0);

            // Everything above the bottom of the removed segment is far off-screen - return it to the pools
            float removedSegmentBottom = oldWalls.transform.position.y - segmentHeight;
            if (PlatformFactory.Instance != null)
            {
                PlatformFactory.Instance.ReleaseObjectsAbove(removedSegmentBottom);
            }

            Destroy(oldWalls);
        }
    }
}