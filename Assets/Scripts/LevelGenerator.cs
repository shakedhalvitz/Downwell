using System.Collections.Generic;
using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    [Header("Chunk Prefabs")]
    public GameObject[] chunkPrefabs;

    [Header("Player Tracking")]
    public Transform playerTransform;

    [Header("Settings")]
    public float chunkHeight = 10f;
    public int chunksAhead = 3;

    private float currentSpawnY = 0f;
    private List<GameObject> activeChunks = new List<GameObject>();

    void Start()
    {
        for (int i = 0; i < chunksAhead + 2; i++)
        {
            SpawnChunk();
        }
    }

    void Update()
    {
        if (playerTransform == null) return;

        if (playerTransform.position.y < currentSpawnY + (chunksAhead * chunkHeight))
        {
            SpawnChunk();
            CleanupOldChunks();
        }
    }

    void SpawnChunk()
    {
        if (chunkPrefabs == null || chunkPrefabs.Length == 0) return;

        int randomIndex = Random.Range(0, chunkPrefabs.Length);
        GameObject selectedPrefab = chunkPrefabs[randomIndex];

        Vector3 spawnPosition = new Vector3(0f, currentSpawnY, 0f);
        GameObject newChunk = Instantiate(selectedPrefab, spawnPosition, Quaternion.identity);

        activeChunks.Add(newChunk);

        currentSpawnY -= chunkHeight;
    }

    void CleanupOldChunks()
    {
        if (activeChunks.Count > 5)
        {
            GameObject oldChunk = activeChunks[0];
            activeChunks.RemoveAt(0);
            Destroy(oldChunk);
        }
    }
}