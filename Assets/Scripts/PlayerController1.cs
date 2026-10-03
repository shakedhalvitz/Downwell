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
    [SerializeField] private InputAction moveAction;
    [SerializeField] private InputAction jumpAction;

    [Header("Combat Settings")]
    [SerializeField] private Transform firePoint;

    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer spriteRenderer;

    private float moveInput;
    private bool isGrounded;

    private GameObject currentBreakablePlatform;
    private GameObject targetBreakablePlatform;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        rb.gravityScale = 3f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        if (firePoint == null)
        {
            firePoint = transform;
        }
    }

    void OnEnable()
    {
        moveAction.Enable();
        jumpAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
        jumpAction.Disable();
    }

    void Update()
    {
        moveInput = moveAction.ReadValue<float>();

        if (moveInput > 0) spriteRenderer.flipX = false;
        else if (moveInput < 0) spriteRenderer.flipX = true;

        anim.SetBool("isGrounded", isGrounded);
        anim.SetBool("isMoving", Mathf.Abs(moveInput) > 0.1f);

        if (jumpAction.WasPressedThisFrame())
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
        if (collision.gameObject.CompareTag("Platform"))
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
                    // Trigger the animation instead of calling Destroy
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

    public void Die()
    {
        anim.SetTrigger("Die");
        this.enabled = false;
    }
}