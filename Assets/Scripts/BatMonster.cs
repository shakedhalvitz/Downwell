using UnityEngine;
using System;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
public class BatMonster : MonoBehaviour
{
    [Header("Flight Settings")]
    [SerializeField] private float flightSpeed = 3f;
    [SerializeField] private float obstacleCheckDistance = 1.5f;
    [SerializeField] private LayerMask obstacleLayer;

    private Rigidbody2D rb;
    private Animator animator;
    private Action<GameObject> returnToPoolCallback;

    private Vector2 currentDirection;
    private bool isDead = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        // Ensure gravity doesn't affect the bat so it can fly freely
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    public void Setup(Action<GameObject> releaseAction)
    {
        returnToPoolCallback = releaseAction;
        isDead = false;
        PickRandomDirection();
    }

    private void FixedUpdate()
    {
        if (isDead) return;

        CheckForObstacles();
        Move();
    }

    private void Move()
    {
        // Using linearVelocity per Unity's new physics standard
        rb.linearVelocity = currentDirection * flightSpeed;

        // Flip sprite to face the flying direction
        if (currentDirection.x != 0)
        {
            float sign = currentDirection.x > 0 ? 1f : -1f;
            transform.localScale = new Vector3(sign, 1, 1);
        }
    }

    private void CheckForObstacles()
    {
        // Shoot a raycast in the current flight direction to detect walls/floors
        RaycastHit2D hit = Physics2D.Raycast(transform.position, currentDirection, obstacleCheckDistance, obstacleLayer);

        // Pick a new direction immediately if an obstacle is close
        if (hit.collider != null)
        {
            PickRandomDirection();
        }
    }

    private void PickRandomDirection()
    {
        // Pick a random direction in 360 degrees
        currentDirection = UnityEngine.Random.insideUnitCircle.normalized;

        // Fallback in case of absolute zero
        if (currentDirection == Vector2.zero)
        {
            currentDirection = Vector2.right;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isDead) return;

        // Player stomp check
        if (collision.CompareTag("Player"))
        {
            if (collision.transform.position.y > transform.position.y + 0.2f)
            {
                Die();
            }
        }
        // Bullet hit check
        else if (collision.CompareTag("Bullet"))
        {
            Die();
        }
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;
        rb.linearVelocity = Vector2.zero;

        animator.SetTrigger("Die");
    }

    /// <summary>
    /// Must be called via Animation Event on the last frame of the death animation
    /// </summary>
    public void OnDeathAnimationComplete()
    {
        returnToPoolCallback?.Invoke(gameObject);
    }
}