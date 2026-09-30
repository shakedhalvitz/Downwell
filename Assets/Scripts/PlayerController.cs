using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Physics")]
    [SerializeField] private Rigidbody2D _rigidbody2D;

    [Header("Movement Settings")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _jumpForce = 12f;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference _moveAction;
    [SerializeField] private InputActionReference _jumpAction;

    private float _horizontalInput;

    private void OnEnable()
    {
        if (_moveAction != null) _moveAction.action.Enable();
        if (_jumpAction != null)
        {
            _jumpAction.action.Enable();
            // Subscribe to the jump button press event
            _jumpAction.action.performed += OnJump;
        }
    }

    private void OnDisable()
    {
        if (_moveAction != null) _moveAction.action.Disable();
        if (_jumpAction != null)
        {
            _jumpAction.action.Disable();
            // Unsubscribe from the jump event
            _jumpAction.action.performed -= OnJump;
        }
    }

    private void Update()
    {
        if (_moveAction != null)
        {
            // Read the Vector2 value from the input system (supports both keyboard and gamepad sticks)
            var moveVector = _moveAction.action.ReadValue<Vector2>();
            _horizontalInput = moveVector.x;
        }
    }

    private void FixedUpdate()
    {
        // Maintain current vertical velocity while overriding horizontal velocity
        _rigidbody2D.linearVelocity = new Vector2(_horizontalInput * _moveSpeed, _rigidbody2D.linearVelocity.y);
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        // Reset vertical velocity before jumping to ensure consistent jump heights
        _rigidbody2D.linearVelocity = new Vector2(_rigidbody2D.linearVelocity.x, 0);
        _rigidbody2D.AddForce(Vector2.up * _jumpForce, ForceMode2D.Impulse);
    }
}