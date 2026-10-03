using System;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CollectableItem : MonoBehaviour
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
            // TODO: Play sound based on type
            // TODO: Notify GameManager to increase score or lives based on ItemType and valueAmount

            // Return the item to the pool instead of destroying it
            returnToPoolCallback?.Invoke(gameObject);
        }
    }
}