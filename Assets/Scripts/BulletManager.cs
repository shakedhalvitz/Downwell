using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class BulletManager : MonoBehaviour
{
    public static BulletManager Instance { get; private set; }
    public static bool HasInstance => Instance != null;

    [SerializeField] private Bullet _bulletPrefab;
    [SerializeField] private int _defaultCapacity = 10;
    [SerializeField] private int _maxCapacity = 50;

    private ObjectPool<Bullet> _bulletPool;

    private void Awake()
    {
        // Ensure Singleton instance is set up immediately before any other script accesses it
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitializePool();
    }

    private void InitializePool()
    {
        _bulletPool = new ObjectPool<Bullet>(
            createFunc: () => Instantiate(_bulletPrefab),
            actionOnGet: OnGet,
            actionOnRelease: OnRelease,
            actionOnDestroy: Destroy,
            collectionCheck: true,
            defaultCapacity: _defaultCapacity,
            maxSize: _maxCapacity
        );

        // Pre-warm the pool to avoid runtime lag spikes
        var bullets = new List<Bullet>();
        for (var i = 0; i < _defaultCapacity; i++)
        {
            bullets.Add(_bulletPool.Get());
        }
        foreach (var bullet in bullets)
        {
            _bulletPool.Release(bullet);
        }
    }

    private void OnGet(Bullet bullet)
    {
        bullet.gameObject.SetActive(true);
    }

    private void OnRelease(Bullet bullet)
    {
        var bulletRigidBody = bullet.GetComponent<Rigidbody2D>();
        if (bulletRigidBody != null)
        {
            bulletRigidBody.linearVelocity = Vector2.zero;
        }
        bullet.gameObject.SetActive(false);
    }

    public Bullet GetBullet()
    {
        return _bulletPool.Get();
    }

    public void ReleaseBullet(Bullet bullet)
    {
        if (bullet.isActiveAndEnabled)
        {
            _bulletPool.Release(bullet);
        }
    }
}