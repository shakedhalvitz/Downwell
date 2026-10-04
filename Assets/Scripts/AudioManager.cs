using UnityEngine;

/// <summary>
/// Plays one-shot sound effects through a single 2D AudioSource.
/// No scene setup needed - the Singleton creates this object the first time Instance is used.
/// Playing from here (instead of from the object itself) means the sound isn't cut off
/// when a collectable or enemy is deactivated and returned to its pool.
/// </summary>
public class AudioManager : Singleton<AudioManager>
{
    private AudioSource _sfxSource;

    private void Awake()
    {
        _sfxSource = gameObject.AddComponent<AudioSource>();
        _sfxSource.playOnAwake = false;
        _sfxSource.spatialBlend = 0f; // 2D - same volume no matter where the camera is
    }

    public void PlaySfx(AudioClip clip, float volume = 1f)
    {
        if (clip == null) return;

        _sfxSource.PlayOneShot(clip, volume);
    }
}
