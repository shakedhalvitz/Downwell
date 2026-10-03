using UnityEngine;
using System;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
public class SlimeMonster : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundDetectionPoint;

    private Rigidbody2D rb;
    private Animator animator;
    private Action<GameObject> returnToPoolCallback;

    private int currentDirection = 1; // 1 for Right, -1 for Left
    private bool isDead = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    /// <summary>
    /// Called by the Factory right after getting the object from the pool.
    /// </summary>
    public void Setup(Action<GameObject> releaseAction)
    {
        returnToPoolCallback = releaseAction;
        isDead = false;

        // Randomize initial walking direction
        currentDirection = UnityEngine.Random.value > 0.5f ? 1 : -1;
        UpdateSpriteDirection();
    }

    private void FixedUpdate()
    {
        if (isDead) return;

        Move();
        CheckLedge();
    }

    private void Move()
    {
        // Using linearVelocity instead of the obsolete velocity
        rb.linearVelocity = new Vector2(currentDirection * moveSpeed, rb.linearVelocity.y);
    }

    private void CheckLedge()
    {
        if (groundDetectionPoint == null) return;

        // Simple downward raycast to check if there is ground ahead
        RaycastHit2D groundInfo = Physics2D.Raycast(groundDetectionPoint.position, Vector2.down, 0.5f, groundLayer);

        // If there is no ground ahead, turn around to avoid falling off the edge
        if (groundInfo.collider == false)
        {
            FlipDirection();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;

        // Turn around if hitting a wall or another enemy
        if (collision.gameObject.CompareTag("Walls") || collision.gameObject.CompareTag("Enemy"))
        {
            FlipDirection();
        }
        // Handle Player Stomp (assuming player is above)
        else if (collision.gameObject.CompareTag("Player"))
        {
            if (collision.GetContact(0).normal.y < -0.5f)
            {
                Die();
            }
        }
    }

    private void FlipDirection()
    {
        currentDirection *= -1;
        UpdateSpriteDirection();
    }

    private void UpdateSpriteDirection()
    {
        transform.localScale = new Vector3(currentDirection, 1, 1);
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;
        rb.linearVelocity = Vector2.zero;

        animator.SetTrigger("Die");
    }

    /// <summary>
    /// This MUST be called via an Animation Event on the very last frame of the death animation.
    /// </summary>
    public void OnDeathAnimationComplete()
    {
        returnToPoolCallback?.Invoke(gameObject);
    }
}