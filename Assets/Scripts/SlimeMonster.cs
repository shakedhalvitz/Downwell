using UnityEngine;
using System;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
public class SlimeMonster : MonoBehaviour
{
    [Header("Score")]
    [SerializeField] private int scoreValue = 3;

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundDetectionPoint;

    private Rigidbody2D rb;
    private Animator animator;
    private Action<GameObject> returnToPoolCallback;

    private int currentDirection = 1; // 1 for Right, -1 for Left
    private bool isDead = false;

    public bool IsDead => isDead;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        // No friction so the slime slides smoothly across the separate platform tiles
        rb.sharedMaterial = new PhysicsMaterial2D("SlimeNoFriction") { friction = 0f, bounciness = 0f };
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
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

        // Only check ledges while standing on ground - otherwise (spawning, falling) it flips every frame
        if (!rb.IsTouchingLayers(groundLayer)) return;

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
        // (Player stomps are handled by PlayerController)
        if (collision.gameObject.CompareTag("Walls") || collision.gameObject.CompareTag("Enemy"))
        {
            FlipDirection();
        }
    }

    private void FlipDirection()
    {
        currentDirection *= -1;
        UpdateSpriteDirection();
    }

    private void UpdateSpriteDirection()
    {
        Vector3 scale = transform.localScale;
        transform.localScale = new Vector3(Mathf.Abs(scale.x) * currentDirection, scale.y, scale.z);
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;
        rb.linearVelocity = Vector2.zero;

        if (GameManager.HasInstance)
        {
            GameManager.Instance.AddScore(scoreValue);
        }

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