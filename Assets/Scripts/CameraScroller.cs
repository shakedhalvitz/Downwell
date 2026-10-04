using UnityEngine;

/// <summary>
/// Moves the camera target down at a constant (slowly increasing) speed while playing.
/// The Cinemachine camera follows this object. If the player falls faster than the scroll,
/// the camera catches up; it never moves back up.
/// Being pushed above the top edge of the screen ends the run.
/// Replaces VerticalOnlyFollow on the CameraFollowTarget object.
/// </summary>
public class CameraScroller : MonoBehaviour
{
    public static CameraScroller Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Transform player;
    [Tooltip("Optional - uses Camera.main if empty")]
    [SerializeField] private Camera mainCamera;

    [Header("Scroll Speed")]
    [SerializeField] private float startScrollSpeed = 2.5f;
    [SerializeField] private float speedIncreasePerSecond = 0.05f;
    [SerializeField] private float maxScrollSpeed = 6f;

    [Header("Top Edge")]
    [Tooltip("How far below the top edge of the screen the player dies")]
    [SerializeField] private float topDeathMargin = 0.2f;

    private float lockedX;
    private float lockedZ;
    private float currentScrollSpeed;
    private PlayerController playerController;

    public float CurrentScrollSpeed => currentScrollSpeed;

    /// <summary>
    /// World Y of the top edge of what the camera currently shows.
    /// </summary>
    public float TopEdgeY => mainCamera != null
        ? mainCamera.transform.position.y + mainCamera.orthographicSize
        : transform.position.y;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (player != null) playerController = player.GetComponent<PlayerController>();

        lockedX = transform.position.x;
        lockedZ = transform.position.z;
        currentScrollSpeed = startScrollSpeed;

        if (player != null)
        {
            transform.position = new Vector3(lockedX, player.position.y, lockedZ);
        }
    }

    private void Update()
    {
        GameManager gm = GameManager.Instance;

        // Only scroll during a run, and pause while the player is dead/respawning
        if (!gm.IsPlaying || !gm.PlayerAlive || player == null) return;

        currentScrollSpeed = Mathf.Min(maxScrollSpeed, currentScrollSpeed + speedIncreasePerSecond * Time.deltaTime);

        float targetY = transform.position.y - currentScrollSpeed * Time.deltaTime;

        // Catch up if the player falls faster than the scroll (never move up)
        targetY = Mathf.Min(targetY, player.position.y);

        transform.position = new Vector3(lockedX, targetY, lockedZ);

        CheckTopEdge(gm);
    }

    private void CheckTopEdge(GameManager gm)
    {
        if (player.position.y <= TopEdgeY - topDeathMargin) return;

        if (playerController != null)
        {
            playerController.Die();
        }
        gm.KillPlayerInstantly();
    }
}
