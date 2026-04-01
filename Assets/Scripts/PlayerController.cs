using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using TouchPhase = UnityEngine.TouchPhase;

public class PlayerController : MonoBehaviour
{
    private CharacterController controller;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float turningSpeed = 10f;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 5f;

    [Header("Physics")]
    [SerializeField] private float gravity = 9.81f;

    [Header("Interact & Look")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float tapMoveThreshold = 15f;

    [Header("Camera Look")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float lookSensitivity = 0.5f;
    [SerializeField] private float maxPitchUp = 60f;
    [SerializeField] private float maxPitchDown = -30f;

    // Input
    private PlayerInputControl inputActions;
    private Vector2 moveInput;
    private float verticalVelocity;

    // Look state
    private float yaw = 0f;
    private float pitch = 0f;

    // Finger tracking — pakai New Input System touch
    private int activeFingerId = -1;
    private Vector2 fingerStartPos;
    private Vector2 fingerLastPos;
    private bool fingerMoved = false;

    [Header("Visual Feedback (Opsional)")]
    [SerializeField] private GameObject tapIndicatorPrefab;
    private GameObject currentIndicator;

    // ──────────────────────────────────────────────
    //  INIT
    // ──────────────────────────────────────────────

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
        if (cameraTransform == null && mainCamera != null)
            cameraTransform = mainCamera.transform;

        if (cameraTransform != null)
        {
            yaw = cameraTransform.eulerAngles.y;
            pitch = cameraTransform.eulerAngles.x;
        }
    }

    // ──────────────────────────────────────────────
    //  UPDATE
    // ──────────────────────────────────────────────

    private void Update()
    {
        HandleLookAndInteract();
        MoveWithJoystick();
        ApplyGravity();
    }

    // ──────────────────────────────────────────────
    //  JOYSTICK MOVEMENT
    // ──────────────────────────────────────────────

    private void MoveWithJoystick()
    {
        moveInput = inputActions.Player.Move.ReadValue<Vector2>();

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        if (moveInput.sqrMagnitude < 0.01f) return;

        Vector3 moveDir = (transform.forward * moveInput.y +
                           transform.right * moveInput.x).normalized;
        Vector3 move = moveDir * walkSpeed;
        move.y = verticalVelocity;
        controller.Move(move * Time.deltaTime);
    }

    private void OnJump(InputAction.CallbackContext ctx)
    {
        if (controller.isGrounded)
            verticalVelocity = jumpForce;
    }

    // ──────────────────────────────────────────────
    //  LOOK + INTERACT — pakai New Input System touch
    // ──────────────────────────────────────────────

    private void HandleLookAndInteract()
    {
#if UNITY_EDITOR
        // Editor: mouse kanan = look, mouse kiri = interact
        var mouse = Mouse.current;
        if (mouse != null)
        {
            if (mouse.rightButton.isPressed)
            {
                Vector2 mouseDelta = mouse.delta.ReadValue();
                ApplyLookDelta(mouseDelta.x * 5f, mouseDelta.y * 5f);
            }
            if (mouse.leftButton.wasReleasedThisFrame)
                TryInteract(mouse.position.ReadValue());
        }
        return; // Editor tidak pakai touch
#endif

        var touchscreen = Touchscreen.current;
        if (touchscreen == null) return;

        foreach (TouchControl touch in touchscreen.touches)
        {
            TouchPhase phase = (TouchPhase)touch.phase.ReadValue();
            int fingerId = touch.touchId.ReadValue();
            Vector2 pos = touch.position.ReadValue();

            switch (phase)
            {
                case TouchPhase.Began:
                    if (activeFingerId == -1)
                    {
                        activeFingerId = fingerId;
                        fingerStartPos = pos;
                        fingerLastPos = pos;
                        fingerMoved = false;
                    }
                    break;

                case TouchPhase.Moved:
                    if (fingerId == activeFingerId)
                    {
                        float dist = Vector2.Distance(pos, fingerStartPos);
                        if (dist > tapMoveThreshold) fingerMoved = true;

                        if (fingerMoved)
                            ApplyLookDelta(pos.x - fingerLastPos.x, pos.y - fingerLastPos.y);

                        fingerLastPos = pos;
                    }
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (fingerId == activeFingerId)
                    {
                        if (!fingerMoved) TryInteract(fingerStartPos);
                        activeFingerId = -1;
                        fingerMoved = false;
                    }
                    break;
            }
        }
    }

    // ──────────────────────────────────────────────
    //  INTERACT
    // ──────────────────────────────────────────────

    private void TryInteract(Vector2 screenPos)
    {
        Ray ray = mainCamera.ScreenPointToRay(screenPos);

        if (!Physics.Raycast(ray, out RaycastHit hit, 100f,
                Physics.AllLayers, QueryTriggerInteraction.Collide))
            return;

        IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
        if (interactable != null)
        {
            Debug.Log($"[Player] Interact → {interactable.DisplayName}");
            interactable.Interact();
            return;
        }

        if (IsGround(hit))
            Debug.Log("[Player] Tap ke lantai – tidak ada aksi.");
        else
            Debug.Log($"[Player] Tap ke objek: {hit.collider.gameObject.name} (tidak interactable)");
    }

    // ──────────────────────────────────────────────
    //  HELPERS
    // ──────────────────────────────────────────────

    private bool IsGround(RaycastHit hit)
    {
        if (groundLayer != 0)
            return (groundLayer.value & (1 << hit.collider.gameObject.layer)) != 0;
        return hit.collider.CompareTag("Ground");
    }

    private void ApplyLookDelta(float deltaX, float deltaY)
    {
        if (cameraTransform == null) return;

        yaw += deltaX * lookSensitivity;
        pitch -= deltaY * lookSensitivity;
        pitch = Mathf.Clamp(pitch, maxPitchDown, maxPitchUp);

        cameraTransform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    private void ApplyGravity()
    {
        if (controller.isGrounded)
            verticalVelocity = Mathf.Max(verticalVelocity, -1f);
        else
            verticalVelocity -= gravity * Time.deltaTime;
    }
}