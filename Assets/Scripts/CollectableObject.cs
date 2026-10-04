using System;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CollectableObject : MonoBehaviour
{
    // Defines the types of collectables available
    public enum ItemType { Coin, Diamond, Heart }

    [Header("Collectable Settings")]
    [Tooltip("Select what type of item this is")]
    public ItemType type;

    [Tooltip("How much score or health this item gives")]
    public int valueAmount = 1;

    // public AudioClip collectSound; // We will use this when we implement sounds

    private Action<GameObject> returnToPoolCallback;

    /// <summary>
    /// Called by the PlatformFactory when the item is spawned from the pool.
    /// </summary>
    public void Setup(Action<GameObject> releaseAction)
    {
        returnToPoolCallback = releaseAction;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (GameManager.Instance != null)
            {
                if (type == ItemType.Heart)
                {
                    // Note: We will need to add this method to GameManager next
                    GameManager.Instance.AddLife();
                }
                else
                {
                    GameManager.Instance.AddScore(valueAmount);
                }
            }

            // TODO: Play sound based on type
            // Return the item to the pool instead of destroying it
            returnToPoolCallback?.Invoke(gameObject);
        }
    }
}