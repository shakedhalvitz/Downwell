using UnityEngine;

public class SegmentTrigger : MonoBehaviour
{
    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {

        if (!hasTriggered && collision.CompareTag("Player"))
        {
            hasTriggered = true;
            LevelGenerator.Instance.SpawnNextSegment();
        }
    }
}