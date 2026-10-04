using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float breakJumpForce = 8.5f;
    [SerializeField] private float hoverForce = 1f;

    [Header("Audio")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _shootSound;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;

    [Header("Combat Settings")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private float stompBounceForce = 8f;

    [Header("Respawn Settings")]
    [Tooltip("No enemy may be closer than this to the respawn point")]
    [SerializeField] private float respawnSafeRadius = 2f;
    [Tooltip("How far up to move on each attempt when the death spot isn't safe")]
    [SerializeField] private float respawnSearchStep = 0.5f;
    [SerializeField] private int respawnMaxAttempts = 30;
    [SerializeField] private float invincibilityDuration = 2f;
    [SerializeField] private float blinkInterval = 0.1f;

    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer spriteRenderer;
    private Collider2D bodyCollider;
    private Vector2 bodySize = Vector2.one * 0.5f;
    private Vector2 bodyOffset = Vector2.zero;

    private float moveInput;
    private bool isGrounded;
    private bool isDead = false;
    private bool isInvincible = false;

    private GameObject currentBreakablePlatform;
    private GameObject targetBreakablePlatform;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        bodyCollider = GetComponent<Collider2D>();

        // Cached because collider bounds aren't reliable while the body is not simulated (during death)
        if (bodyCollider != null)
        {
            bodySize = bodyCollider.bounds.size;
            bodyOffset = bodyCollider.bounds.center - transform.position;
        }

        rb.gravityScale = 3f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        if (firePoint == null)
        {
            firePoint = transform;
        }
    }

    void Start()
    {
        // Subscribed here (not OnEnable) because Die() disables this component, and we still need to hear the respawn
        GameManager.Instance.OnPlayerRespawned += Respawn;
    }

    void OnDestroy()
    {
        if (GameManager.HasInstance)
        {
            GameManager.Instance.OnPlayerRespawned -= Respawn;
        }
    }

    void OnEnable()
    {
        if (moveAction != null) moveAction.action.Enable();
        if (jumpAction != null) jumpAction.action.Enable();
    }

    void OnDisable()
    {
        if (moveAction != null) moveAction.action.Disable();
        if (jumpAction != null) jumpAction.action.Disable();
    }

    void Update()
    {
        if (isDead) return;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateDepth(transform.position.y);
        }

        if (moveAction != null)
        {
            // Here is the fix: Read Vector2 first, then extract the X axis
            Vector2 moveVector = moveAction.action.ReadValue<Vector2>();
            moveInput = moveVector.x;
        }

        if (moveInput > 0) spriteRenderer.flipX = false;
        else if (moveInput < 0) spriteRenderer.flipX = true;

        anim.SetBool("isGrounded", isGrounded);
        anim.SetBool("isMoving", Mathf.Abs(moveInput) > 0.1f);

        if (jumpAction != null && jumpAction.action.WasPressedThisFrame())
        {
            if (isGrounded)
            {
                Jump();
            }
            else
            {
                ShootDownward();
            }
        }
    }

    void FixedUpdate()
    {
        if (isDead) return;
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);
    }

    private void Jump()
    {
        if (currentBreakablePlatform != null)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, breakJumpForce);
            targetBreakablePlatform = currentBreakablePlatform;
            currentBreakablePlatform = null;

            anim.SetTrigger("BreakJump");
        }
        else
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            targetBreakablePlatform = null;

            anim.SetTrigger("Jump");
        }
    }

    private void ShootDownward()
    {
        if (BulletManager.HasInstance)
        {
            Bullet bullet = BulletManager.Instance.GetBullet();
            if (bullet != null)
            {
                bullet.transform.position = firePoint.position;
                bullet.Fire(Vector2.down);
                if (_audioSource != null && _shootSound != null)
                {
                    _audioSource.PlayOneShot(_shootSound);
                }
            }
        }

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, hoverForce);
        anim.SetTrigger("Jump");
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;

        if (collision.gameObject.CompareTag("Enemy"))
        {
            HandleEnemyContact(collision.collider);
        }
        else if (collision.gameObject.CompareTag("Platform"))
        {
            isGrounded = true;
            currentBreakablePlatform = null;
            targetBreakablePlatform = null;
        }
        else if (collision.gameObject.CompareTag("BreakablePlatform"))
        {
            if (transform.position.y > collision.transform.position.y)
            {
                if (collision.gameObject == targetBreakablePlatform)
                {
                    BreakablePlatform breakable = collision.gameObject.GetComponent<BreakablePlatform>();
                    if (breakable != null)
                    {
                        breakable.BreakAndDeactivate();
                    }
                    else
                    {
                        collision.gameObject.SetActive(false);
                    }

                    targetBreakablePlatform = null;
                }
                else
                {
                    isGrounded = true;
                    currentBreakablePlatform = collision.gameObject;
                    targetBreakablePlatform = null;
                }
            }
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Platform") || collision.gameObject.CompareTag("BreakablePlatform"))
        {
            isGrounded = false;

            if (collision.gameObject == currentBreakablePlatform)
            {
                currentBreakablePlatform = null;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isDead) return;

        // Enemy triggers can sit on untagged child objects (e.g. the bat), so check the owning rigidbody too
        if (collision.CompareTag("Enemy") || (collision.attachedRigidbody != null && collision.attachedRigidbody.CompareTag("Enemy")))
        {
            HandleEnemyContact(collision);
        }
    }

    /// <summary>
    /// Single place that decides who wins a player-vs-enemy contact:
    /// landing on top of the enemy kills it, any other contact hurts the player.
    /// </summary>
    private void HandleEnemyContact(Collider2D enemyCollider)
    {
        GameObject enemy = enemyCollider.attachedRigidbody != null ? enemyCollider.attachedRigidbody.gameObject : enemyCollider.gameObject;

        SlimeMonster slime = enemy.GetComponent<SlimeMonster>();
        BatMonster bat = enemy.GetComponent<BatMonster>();

        // Enemies that are already playing their death animation are harmless
        if ((slime != null && slime.IsDead) || (bat != null && bat.IsDead)) return;

        bool isFalling = rb.linearVelocity.y <= 0.1f;
        bool isAbove = bodyCollider != null && bodyCollider.bounds.min.y >= enemyCollider.bounds.center.y;

        if (isFalling && isAbove)
        {
            if (slime != null) slime.Die();
            if (bat != null) bat.Die();

            rb.linearVelocity = new Vector2(rb.linearVelocity.x, stompBounceForce);
            anim.SetTrigger("Jump");
            return;
        }

        if (isInvincible) return;

        Die();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerHit();
        }
    }

    public void Die()
    {
        if (isDead) return;

        isDead = true;
        anim.SetTrigger("Die");
        rb.linearVelocity = Vector2.zero;

        // Freeze physics so the body doesn't keep falling or touching things while dead
        rb.simulated = false;
        this.enabled = false;
    }

    private void Respawn()
    {
        transform.position = FindSafeRespawnPosition(transform.position);

        isDead = false;
        isGrounded = false;
        currentBreakablePlatform = null;
        targetBreakablePlatform = null;
        moveInput = 0f;

        rb.simulated = true;
        rb.linearVelocity = Vector2.zero;

        // Reset the animator back to its default (idle) state
        anim.Rebind();
        anim.Update(0f);

        this.enabled = true;
        StartCoroutine(InvincibilityRoutine());
    }

    /// <summary>
    /// Starts at the death spot and moves upward until there is no enemy nearby
    /// and the player's body doesn't overlap any solid object.
    /// </summary>
    private Vector2 FindSafeRespawnPosition(Vector2 origin)
    {
        Vector2 candidate = origin;

        for (int i = 0; i < respawnMaxAttempts; i++)
        {
            if (IsSafeRespawnPosition(candidate))
            {
                return candidate;
            }
            candidate += Vector2.up * respawnSearchStep;
        }

        // Nothing safe found - fall back to the death spot and rely on invincibility
        return origin;
    }

    private bool IsSafeRespawnPosition(Vector2 position)
    {
        foreach (Collider2D hit in Physics2D.OverlapCircleAll(position, respawnSafeRadius))
        {
            if (hit.attachedRigidbody == rb) continue;

            bool isEnemy = hit.CompareTag("Enemy") || (hit.attachedRigidbody != null && hit.attachedRigidbody.CompareTag("Enemy"));
            if (isEnemy) return false;
        }

        // Don't spawn inside a platform or wall
        foreach (Collider2D hit in Physics2D.OverlapBoxAll(position + bodyOffset, bodySize, 0f))
        {
            if (hit.attachedRigidbody == rb || hit.isTrigger) continue;
            return false;
        }

        return true;
    }

    private IEnumerator InvincibilityRoutine()
    {
        isInvincible = true;

        float elapsed = 0f;
        while (elapsed < invincibilityDuration)
        {
            spriteRenderer.enabled = !spriteRenderer.enabled;
            yield return new WaitForSeconds(blinkInterval);
            elapsed += blinkInterval;
        }

        spriteRenderer.enabled = true;
        isInvincible = false;
    }
}