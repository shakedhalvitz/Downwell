using System.Collections;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    private static readonly int ExplodeHash = Animator.StringToHash("Explode");

    [SerializeField] private Rigidbody2D _rigidbody2D;
    [SerializeField] private Collider2D _collider;
    [SerializeField] private float _power = 10f;
    [SerializeField] private float _timeout = 2f;

    private Animator _animator;

    private void Awake()
    {
        // Automatically caches the Animator attached to this prefab
        _animator = GetComponent<Animator>();
    }

    public Collider2D Collider => _collider;

    public void Fire(Vector2 direction)
    {
        // Restore dynamic body type so it can fall and move freely after being pulled from the pool
        _rigidbody2D.bodyType = RigidbodyType2D.Dynamic;

        _collider.enabled = true;
        var firePower = direction.normalized * _power;
        _rigidbody2D.AddForce(firePower, ForceMode2D.Impulse);

        StartCoroutine(TimeoutRoutine());
    }

    private IEnumerator TimeoutRoutine()
    {
        yield return new WaitForSeconds(_timeout);
        ReleaseToPool();
    }

    private void ReleaseToPool()
    {
        if (BulletManager.HasInstance)
        {
            BulletManager.Instance.ReleaseBullet(this);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            // Bullets are the single place that handles shooting enemies
            SlimeMonster slime = collision.gameObject.GetComponent<SlimeMonster>();
            if (slime != null) slime.Die();

            BatMonster bat = collision.gameObject.GetComponent<BatMonster>();
            if (bat != null) bat.Die();

            Explode();
        }
        else if (collision.gameObject.CompareTag("BreakablePlatform"))
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

            Explode();
        }
        else if (collision.gameObject.CompareTag("Platform") || collision.gameObject.CompareTag("Walls"))
        {
            Explode();
        }
    }

    private void Explode()
    {
        // Stop all movement and freeze physics during the explosion animation
        _rigidbody2D.linearVelocity = Vector2.zero;
        _rigidbody2D.angularVelocity = 0f;
        _rigidbody2D.bodyType = RigidbodyType2D.Kinematic;

        if (_animator != null)
        {
            _animator.SetTrigger(ExplodeHash);
        }

        StopAllCoroutines();
        StartCoroutine(WaitAndRelease(0.4f));
    }

    private IEnumerator WaitAndRelease(float delay)
    {
        yield return new WaitForSeconds(delay);
        ReleaseToPool();
    }
}