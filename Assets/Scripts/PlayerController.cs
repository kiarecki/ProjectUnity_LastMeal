using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Walking speed in m/s")]
    public float walkSpeed = 3.5f;
    [Tooltip("Sprinting speed in m/s")]
    public float sprintSpeed = 6.0f;
    [Tooltip("Jump height in meters")]
    public float jumpHeight = 1.0f;
    [Tooltip("Gravity acceleration")]
    public float gravity = -18.0f;

    [Header("Look Settings")]
    [Tooltip("Reference to the camera transform")]
    public Transform cameraTransform;
    [Tooltip("Mouse sensitivity multiplier")]
    public float mouseSensitivity = 0.12f;
    [Tooltip("Vertical look angle clamping")]
    public float verticalLookLimit = 85.0f;

    [Header("Flashlight (Optional)")]
    public Light flashlight;

    private CharacterController characterController;
    private Vector3 verticalVelocity;
    private float verticalRotation = 0f;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        // Auto-configure CharacterController defaults if not set
        if (characterController != null)
        {
            characterController.height = 1.8f;
            characterController.radius = 0.35f;
            characterController.center = new Vector3(0, 0.9f, 0);
            characterController.stepOffset = 0.3f;
            characterController.slopeLimit = 45f;
        }

        // Auto-detect camera if unassigned
        if (cameraTransform == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null)
            {
                cameraTransform = cam.transform;
            }
            else if (Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
        }

        if (flashlight == null && cameraTransform != null)
        {
            flashlight = cameraTransform.GetComponentInChildren<Light>();
        }
    }

    private void Start()
    {
        LockCursor();
    }

    private void Update()
    {
        HandleCursorLock();
        HandleLook();
        HandleMovement();
        HandleFlashlight();
    }

    private void HandleCursorLock()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        var mouse = Mouse.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        if (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
        {
            LockCursor();
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
        {
            LockCursor();
        }
#endif
    }

    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void HandleLook()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;

        Vector2 lookDelta = GetLookInput() * mouseSensitivity;

        // Horizontal rotation turns player body
        transform.Rotate(Vector3.up * lookDelta.x);

        // Vertical rotation pitches the camera
        if (cameraTransform != null)
        {
            verticalRotation -= lookDelta.y;
            verticalRotation = Mathf.Clamp(verticalRotation, -verticalLookLimit, verticalLookLimit);
            cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        }
    }

    private void HandleMovement()
    {
        bool isGrounded = characterController.isGrounded;

        if (isGrounded && verticalVelocity.y < 0)
        {
            // Small downward velocity to keep grounded on slopes
            verticalVelocity.y = -2.0f;
        }

        Vector2 input = GetMoveInput();
        bool isSprinting = IsSprintPressed();
        float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;

        Vector3 move = (transform.forward * input.y + transform.right * input.x);
        characterController.Move(move * currentSpeed * Time.deltaTime);

        // Jumping
        if (IsJumpPressed() && isGrounded)
        {
            verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2.0f * gravity);
        }

        // Gravity
        verticalVelocity.y += gravity * Time.deltaTime;
        characterController.Move(verticalVelocity * Time.deltaTime);
    }

    private void HandleFlashlight()
    {
        if (flashlight == null) return;

        bool toggle = false;
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null && kb.fKey.wasPressedThisFrame) toggle = true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.F)) toggle = true;
#endif
        if (toggle)
        {
            flashlight.enabled = !flashlight.enabled;
        }
    }

    private Vector2 GetMoveInput()
    {
        Vector2 input = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) input.y += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) input.y -= 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) input.x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input.x += 1f;
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (input == Vector2.zero)
        {
            input.x = Input.GetAxisRaw("Horizontal");
            input.y = Input.GetAxisRaw("Vertical");
        }
#endif
        return input.sqrMagnitude > 1f ? input.normalized : input;
    }

    private Vector2 GetLookInput()
    {
        Vector2 look = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
        var mouse = Mouse.current;
        if (mouse != null)
        {
            look = mouse.delta.ReadValue();
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (look == Vector2.zero)
        {
            look = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 10f;
        }
#endif
        return look;
    }

    private bool IsSprintPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed)) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) return true;
#endif
        return false;
    }

    private bool IsJumpPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null && kb.spaceKey.wasPressedThisFrame) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Space)) return true;
#endif
        return false;
    }
}
