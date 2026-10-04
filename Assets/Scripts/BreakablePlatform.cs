using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D), typeof(Animator))]
public class BreakablePlatform : MonoBehaviour
{
    private Animator _animator;
    private Collider2D _platformCollider;
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _breakSound;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _platformCollider = GetComponent<Collider2D>();
    }

    public void BreakAndDeactivate()
    {
        if (_platformCollider != null)
        {
            _platformCollider.enabled = false;
        }

        if (_animator != null)
        {
            _animator.SetTrigger("Break");
        }
        if (_audioSource != null && _breakSound != null)
        {
            _audioSource.PlayOneShot(_breakSound);
        }

        StartCoroutine(DeactivateRoutine());
    }

    private IEnumerator DeactivateRoutine()
    {
        yield return new WaitForSeconds(0.4f);

        // Back to the pool so it can be reused further down
        if (PlatformFactory.Instance != null)
        {
            PlatformFactory.Instance.Release(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        if (_platformCollider != null)
        {
            _platformCollider.enabled = true;
        }
    }
}