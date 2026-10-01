using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    [Header("Chunk Prefabs")]
    public GameObject[] chunkPrefabs;

    [Header("Generation Settings")]
    public int startingChunks = 3;
    public float chunkHeight = 10f;

    // הנה המשתנה שהיה חסר!
    public Transform playerTransform;

    private float currentSpawnY = 0f;

    void Start()
    {
        for (int i = 0; i < startingChunks; i++)
        {
            SpawnNextChunk();
        }
    }

    void Update()
    {
        // מוודא שהשחקן קיים כדי למנוע שגיאות
        if (playerTransform == null) return;

        // בודק מול השחקן מתי לייצר את החדר הבא
        if (playerTransform.position.y - currentSpawnY < (chunkHeight * 2))
        {
            SpawnNextChunk();
        }
    }

    void SpawnNextChunk()
    {
        int randomIndex = Random.Range(0, chunkPrefabs.Length);
        GameObject chunkToSpawn = chunkPrefabs[randomIndex];

        Instantiate(chunkToSpawn, new Vector3(0, currentSpawnY, 0), Quaternion.identity);

        currentSpawnY -= chunkHeight;
    }
}