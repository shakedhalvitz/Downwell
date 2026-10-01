using UnityEngine;

public class VerticalOnlyFollow : MonoBehaviour
{
    public Transform player;
    private float lockedX;
    private float lockedZ;

    void Start()
    {
        // Lock the horizontal axes to the initial position of this target object
        lockedX = transform.position.x;
        lockedZ = transform.position.z;
    }

    void LateUpdate()
    {
        if (player != null)
        {
            // Only update the Y axis to match the player
            transform.position = new Vector3(lockedX, player.position.y, lockedZ);
        }
    }
}
