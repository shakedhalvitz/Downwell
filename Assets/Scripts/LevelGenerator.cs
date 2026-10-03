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

    void Start()
    {
        for (int i = 0; i < initialSegments; i++)
        {
            SpawnNextSegment();
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

        if (PlatformFactory.Instance != null)
        {
            float topY = currentSpawnY;
            float bottomY = currentSpawnY - segmentHeight;
            PlatformFactory.Instance.GeneratePlatformsInArea(topY, bottomY);
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
            Destroy(oldWalls);
        }
    }
}