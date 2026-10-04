using UnityEngine;

/// <summary>
/// Keeps this object (e.g. the on-screen touch controls) active only on mobile devices.
/// Tick "Show In Editor" to test the controls in the editor / Device Simulator.
/// </summary>
public class MobileOnly : MonoBehaviour
{
    [SerializeField] private bool showInEditor = false;

    private void Awake()
    {
        bool show = Application.isMobilePlatform || (Application.isEditor && showInEditor);
        gameObject.SetActive(show);
    }
}
