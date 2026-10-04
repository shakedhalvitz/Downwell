using UnityEngine;
using System;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
public class BatMonster : MonoBehaviour
{
    [Header("Score")]
    [SerializeField] private int scoreValue = 5;

    [Header("Flight Settings")]
    [SerializeField] private float flightSpeed = 3f;
    [Tooltip("Random spread (degrees) added to each bounce so the flight path doesn't repeat")]
    [SerializeField] private float bounceRandomAngle = 30f;

    private Rigidbody2D rb;
    private Animator animator;
    private Action<GameObject> returnToPoolCallback;

    private Vector2 currentDirection;
    private bool isDead = false;

    public bool IsDead => isDead;

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

        // Clear any leftover Die trigger, then force the animator back to idle
        animator.ResetTrigger("Die");
        animator.Play("BatIdle", -1, 0f);

        PickRandomDirection();
    }

    private void FixedUpdate()
    {
        if (isDead) return;

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
            Vector3 scale = transform.localScale;
            transform.localScale = new Vector3(Mathf.Abs(scale.x) * sign, scale.y, scale.z);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        BounceOff(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        // Covers cases where the bat is still pushing into a surface after the first bounce
        BounceOff(collision);
    }

    private void BounceOff(Collision2D collision)
    {
        if (isDead || collision.contactCount == 0) return;

        Vector2 normal = collision.GetContact(0).normal;

        // Already flying away from this surface - nothing to do
        if (Vector2.Dot(currentDirection, normal) >= 0f) return;

        // Mirror the direction off the surface, then add a little randomness
        Vector2 reflected = Vector2.Reflect(currentDirection, normal);
        float angle = UnityEngine.Random.Range(-bounceRandomAngle, bounceRandomAngle);
        Vector2 newDirection = (Quaternion.Euler(0f, 0f, angle) * reflected).normalized;

        // Make sure the randomness didn't point us back into the surface
        if (Vector2.Dot(newDirection, normal) < 0.1f)
        {
            newDirection = reflected;
        }

        currentDirection = newDirection;
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

        // Bullet hit check (player stomps are handled by PlayerController)
        if (collision.CompareTag("Bullet"))
        {
            if (collision.transform.position.y > transform.position.y + 0.2f)
            {
                Die();
            }
        }
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
    /// Must be called via Animation Event on the last frame of the death animation
    /// </summary>
    public void OnDeathAnimationComplete()
    {
        returnToPoolCallback?.Invoke(gameObject);
    }
}