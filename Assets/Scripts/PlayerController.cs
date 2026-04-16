using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class PlayerController : MonoBehaviour
{
    // ── Components ──────────────────────────────────
    private CharacterController controller;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float rotateSpeed = 10f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 5f;

    [Header("Physics")]
    [SerializeField] private float gravity = 9.81f;

    [Header("Camera Look")]
    [SerializeField] private float lookSensitivity = 0.15f;
    [SerializeField] private float maxTiltUp = 60f;
    [SerializeField] private float maxTiltDown = -30f;

    [Header("Touch")]
    [SerializeField] private float tapMoveThreshold = 15f;
    [SerializeField] private Camera mainCamera;

    [Header("Interact")]
    [SerializeField] private LayerMask groundLayer;

    // Pan/Tilt value — dibaca oleh TPPCamController.cs di kamera
    public float PanValue { get; private set; }
    public float TiltValue { get; private set; }

    // ── Input ────────────────────────────────────────
    private PlayerInputControl inputActions;
    private Vector2 moveInput;
    private float verticalVelocity;

    // ── Finger tracking ───────────────────────────────
    private int activeFingerId = -1;
    private Vector2 fingerStartPos;
    private Vector2 fingerLastPos;
    private bool fingerMoved = false;

    // ────────────────────────────────────────────────
    //  INIT
    // ────────────────────────────────────────────────

    private void Awake()
    {
        inputActions = new PlayerInputControl();
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.Jump.performed += OnJump;
    }

    private void OnDisable()
    {
        inputActions.Player.Jump.performed -= OnJump;
        inputActions.Player.Disable();
    }

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        if (mainCamera == null) mainCamera = Camera.main;

        // Init PanValue dari rotasi Player saat start
        PanValue = transform.eulerAngles.y;
        TiltValue = 0f;
    }

    // ────────────────────────────────────────────────
    //  UPDATE
    // ────────────────────────────────────────────────

    private void Update()
    {
        HandleTouch();
        MovePlayer();
        ApplyGravity();
    }

    // ────────────────────────────────────────────────
    //  MOVEMENT — arah gerak relatif ke PanValue (yaw kamera)
    // ────────────────────────────────────────────────

    private void MovePlayer()
    {
        moveInput = inputActions.Player.Move.ReadValue<Vector2>();

        if (moveInput.sqrMagnitude >= 0.01f)
        {
            Vector3 camForward = Quaternion.Euler(0f, PanValue, 0f) * Vector3.forward;
            Vector3 camRight = Quaternion.Euler(0f, PanValue, 0f) * Vector3.right;
            Vector3 moveDir = (camForward * moveInput.y + camRight * moveInput.x).normalized;

            Vector3 move = moveDir * walkSpeed;
            move.y = verticalVelocity;
            controller.Move(move * Time.deltaTime);

            // Badan Player rotate smooth mengikuti arah gerak
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(moveDir),
                rotateSpeed * Time.deltaTime
            );
        }
        else
        {
            Vector3 idle = new Vector3(0f, verticalVelocity, 0f);
            controller.Move(idle * Time.deltaTime);
        }
    }

    private void OnJump(InputAction.CallbackContext ctx)
    {
        if (controller.isGrounded)
            verticalVelocity = jumpForce;
    }

    // ────────────────────────────────────────────────
    //  TOUCH
    // ────────────────────────────────────────────────

    private void HandleTouch()
    {
#if UNITY_EDITOR
        var mouse = Mouse.current;
        if (mouse != null)
        {
            if (mouse.rightButton.isPressed)
            {
                Vector2 d = mouse.delta.ReadValue();
                ApplyLookDelta(d.x * 5f, d.y * 5f);
            }
            if (mouse.leftButton.wasReleasedThisFrame)
                TryInteract(mouse.position.ReadValue());
        }
        return;
#endif

        var ts = Touchscreen.current;
        if (ts == null) return;

        foreach (var touch in ts.touches)
        {
            var phase = touch.phase.ReadValue();
            int fid = touch.touchId.ReadValue();
            Vector2 pos = touch.position.ReadValue();

            switch (phase)
            {
                case UnityEngine.InputSystem.TouchPhase.Began:
                    if (activeFingerId == -1)
                    {
                        activeFingerId = fid;
                        fingerStartPos = pos;
                        fingerLastPos = pos;
                        fingerMoved = false;
                    }
                    break;

                case UnityEngine.InputSystem.TouchPhase.Moved:
                    if (fid == activeFingerId)
                    {
                        if (Vector2.Distance(pos, fingerStartPos) > tapMoveThreshold)
                            fingerMoved = true;
                        if (fingerMoved)
                            ApplyLookDelta(pos.x - fingerLastPos.x, pos.y - fingerLastPos.y);
                        fingerLastPos = pos;
                    }
                    break;

                case UnityEngine.InputSystem.TouchPhase.Ended:
                case UnityEngine.InputSystem.TouchPhase.Canceled:
                    if (fid == activeFingerId)
                    {
                        if (!fingerMoved) TryInteract(fingerStartPos);
                        activeFingerId = -1;
                        fingerMoved = false;
                    }
                    break;
            }
        }
    }

    // ────────────────────────────────────────────────
    //  LOOK — update PanValue & TiltValue
    //  TPPCamController.cs yang apply ke Cinemachine
    // ────────────────────────────────────────────────

    private void ApplyLookDelta(float dx, float dy)
    {
        PanValue += dx * lookSensitivity;
        TiltValue -= dy * lookSensitivity;
        TiltValue = Mathf.Clamp(TiltValue, maxTiltDown, maxTiltUp);
    }

    // ────────────────────────────────────────────────
    //  INTERACT
    // ────────────────────────────────────────────────

    private void TryInteract(Vector2 screenPos)
    {
        if (mainCamera == null) return;
        Ray ray = mainCamera.ScreenPointToRay(screenPos);
        if (!Physics.Raycast(ray, out RaycastHit hit, 100f,
                Physics.AllLayers, QueryTriggerInteraction.Collide)) return;

        IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
        if (interactable != null)
        {
            Debug.Log($"[Player] Interact → {interactable.DisplayName}");
            interactable.Interact();
            return;
        }

        if (IsGround(hit))
            Debug.Log("[Player] Tap lantai.");
        else
            Debug.Log($"[Player] Tap objek: {hit.collider.gameObject.name}");
    }

    // ────────────────────────────────────────────────
    //  HELPERS
    // ────────────────────────────────────────────────

    private bool IsGround(RaycastHit hit)
    {
        if (groundLayer != 0)
            return (groundLayer.value & (1 << hit.collider.gameObject.layer)) != 0;
        return hit.collider.CompareTag("Ground");
    }

    private void ApplyGravity()
    {
        if (controller.isGrounded)
            verticalVelocity = Mathf.Max(verticalVelocity, -1f);
        else
            verticalVelocity -= gravity * Time.deltaTime;
    }
}