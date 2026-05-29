using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using InputTouchPhase = UnityEngine.InputSystem.TouchPhase;

/// <summary>
/// PlayerController — GolakGolek v2
///
/// DEPENDENCIES:
///  - CharacterController di GameObject yang sama
///  - PlayerInputControl (generated dari Input Action Asset)
///  - ShelfUnit harus punya static event OnPlayerRangeChanged (lihat catatan di bawah)
///  - HeadPoint: empty child GO di posisi "kepala" player
///  - CameraRoot: empty child GO di posisi badan, yang di-rotate untuk orbit cam
///
/// TOUCH DESIGN (screen split):
///  - Kiri layar  → wilayah joystick, biarkan OnScreenStick yang handle
///  - Kanan layar → PlayerController track untuk camera orbit + tap interact
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    // ──────────────────────────────────────────────
    //  INSPECTOR FIELDS
    // ──────────────────────────────────────────────

    [Header("References")]
    [SerializeField] private Transform cameraRoot;      // Child kosong yg dirotasi untuk orbit camera
    [SerializeField] private Transform headPoint;       // Origin raycast interact (posisi kepala)
    [SerializeField] private Camera mainCamera;
    [SerializeField] private GameObject interactButton; // UI Button, diatur SetActive(true/false)

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 7f;
    [SerializeField] private float bodyTurnSpeed = 9f; // Seberapa cepat badan player balik ke arah gerak

    [Header("Physics")]
    [SerializeField] private float gravity = 9.81f;

    [Header("Camera Orbit")]
    [SerializeField] private float lookSensitivity = 0.3f;
    [SerializeField] private float maxPitchUp = 50f;
    [SerializeField] private float maxPitchDown = -20f;

    [Header("Interact (Raycast dari HeadPoint)")]
    [SerializeField] private float interactRayLength = 3f;
    [SerializeField] private LayerMask interactLayer;   // Layer ProximityDetector collider

    [Header("Touch")]
    [Tooltip("Berapa pixel drift sebelum touch dianggap drag (bukan tap)")]
    [SerializeField] private float tapMoveThreshold = 25f;

    // ──────────────────────────────────────────────
    //  PRIVATE STATE
    // ──────────────────────────────────────────────

    private CharacterController controller;
    private PlayerInputControl inputActions;

    // Movement
    private Vector2 moveInput;
    private float verticalVelocity;

    // Camera orbit
    private float yaw;
    private float pitch;

    // Touch tracking (kanan layar + joystick)
    private int activeFingerId = -1;
    private int joystickFingerId = -1;
    private Vector2 fingerStartPos;
    private Vector2 fingerLastPos;
    private bool fingerMoved;

    // Interact
    private ShelfUnit currentNearbyShelf;
    private bool interactButtonVisible;

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

        // Subscribe ke event ShelfUnit — tau kapan player masuk/keluar zona rak
        ShelfUnit.OnPlayerRangeChanged += HandleShelfRangeChanged;
    }

    private void OnDisable()
    {
        inputActions.Player.Disable();
        ShelfUnit.OnPlayerRangeChanged -= HandleShelfRangeChanged;
    }

    private void Start()
    {
        controller = GetComponent<CharacterController>();

        if (mainCamera == null)
            mainCamera = Camera.main;

        // Inisialisasi yaw dari rotasi saat ini supaya kamera tidak jump
        yaw = transform.eulerAngles.y;

        SetInteractButton(false);
    }

    // ──────────────────────────────────────────────
    //  UPDATE
    // ──────────────────────────────────────────────

    private void Update()
    {
        HandleTouch();
        MovePlayer();
        ApplyGravity();
        ValidateInteract();
    }

    // ──────────────────────────────────────────────
    //  MOVEMENT — relatif ke arah kamera
    // ──────────────────────────────────────────────

    private void MovePlayer()
    {
        moveInput = inputActions.Player.Move.ReadValue<Vector2>();

        Vector3 velocity = Vector3.zero;

        if (moveInput.sqrMagnitude >= 0.01f)
        {
            Vector3 camForward = Vector3.ProjectOnPlane(mainCamera.transform.forward, Vector3.up).normalized;
            Vector3 camRight = Vector3.ProjectOnPlane(mainCamera.transform.right, Vector3.up).normalized;
            Vector3 moveDir = (camForward * moveInput.y + camRight * moveInput.x).normalized;

            if (moveDir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, bodyTurnSpeed * Time.deltaTime);
            }

            velocity = moveDir * walkSpeed;
        }

        // Gravity SELALU diapply, tidak tergantung joystick
        velocity.y = verticalVelocity;
        controller.Move(velocity * Time.deltaTime);
    }


    private void ApplyGravity()
    {
        if (controller.isGrounded)
            verticalVelocity = Mathf.Max(verticalVelocity, -1f);
        else
            verticalVelocity -= gravity * Time.deltaTime;
    }

    // ──────────────────────────────────────────────
    //  CAMERA ORBIT — putar CameraRoot, Cinemachine ikut
    // ──────────────────────────────────────────────

    private void ApplyCameraOrbit(float deltaX, float deltaY)
    {
        if (cameraRoot == null) return;

        yaw += deltaX * lookSensitivity;
        pitch -= deltaY * lookSensitivity;
        pitch = Mathf.Clamp(pitch, maxPitchDown, maxPitchUp);

        cameraRoot.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    // ──────────────────────────────────────────────
    //  TOUCH — kanan layar: camera orbit + tap
    // ──────────────────────────────────────────────

    private void HandleTouch()
    {
#if UNITY_EDITOR
        var mouse = Mouse.current;
        if (mouse != null)
        {
            if (mouse.rightButton.isPressed)
            {
                Debug.Log("[Player] Editor right click");
            }
            if (mouse.leftButton.wasReleasedThisFrame)
                Debug.Log("[Player] Editor left click — interact via button, bukan tap");
        }
        return;
#endif

        var touchscreen = Touchscreen.current;
        if (touchscreen == null) return;

        foreach (TouchControl touch in touchscreen.touches)
        {
            InputTouchPhase phase = touch.phase.ReadValue();
            int fingerId = touch.touchId.ReadValue();
            Vector2 pos = touch.position.ReadValue();

            bool startsLeft = pos.x <= Screen.width * 0.5f;
            bool startsRight = !startsLeft;

            switch (phase)
            {
                case InputTouchPhase.Began:
                    // Claim joystick finger
                    if (startsLeft && joystickFingerId == -1)
                    {
                        joystickFingerId = fingerId;
                    }
                    // Claim look finger — pastikan bukan joystick finger
                    else if (startsRight && activeFingerId == -1
                             && fingerId != joystickFingerId)
                    {
                        activeFingerId = fingerId;
                        fingerStartPos = pos;
                        fingerLastPos = pos;
                        fingerMoved = false;
                    }
                    break;

                case InputTouchPhase.Moved:
                    if (fingerId == activeFingerId)
                    {
                        if (Vector2.Distance(pos, fingerStartPos) > tapMoveThreshold)
                            fingerMoved = true;

                        if (fingerMoved)
                            ApplyCameraOrbit(pos.x - fingerLastPos.x,
                                             pos.y - fingerLastPos.y);

                        fingerLastPos = pos;
                    }
                    break;

                case InputTouchPhase.Ended:
                case InputTouchPhase.Canceled:
                    if (fingerId == joystickFingerId)
                        joystickFingerId = -1;

                    if (fingerId == activeFingerId)
                    {
                        if (!fingerMoved)
                            Debug.Log("[Player] Tap kanan — interact via button");

                        activeFingerId = -1;
                        fingerMoved = false;
                    }
                    break;
            }
        }
    }

    // ──────────────────────────────────────────────
    //  INTERACT — Genshin style
    //  1. ProximityDetector → ShelfUnit.SetPlayerInRange → event
    //  2. PlayerController terima event → simpan currentNearbyShelf
    //  3. Raycast dari HeadPoint ke depan → validasi apakah facing rak
    //  4. Kalau valid → tampilkan Interact Button
    //  5. Player tekan button → OnInteractButtonPressed()
    // ──────────────────────────────────────────────

    private void HandleShelfRangeChanged(ShelfUnit shelf, bool inRange)
    {
        if (inRange)
        {
            currentNearbyShelf = shelf;
        }
        else if (currentNearbyShelf == shelf)
        {
            currentNearbyShelf = null;
            SetInteractButton(false);
        }
    }

    private void ValidateInteract()
    {
        if (currentNearbyShelf == null || headPoint == null)
        {
            SetInteractButton(false);
            return;
        }

        // Raycast dari kepala player ke depan (arah badan)
        // QueryTriggerInteraction.Collide supaya kena collider isTrigger ProximityDetector
        bool hit = Physics.Raycast(
            headPoint.position,
            transform.forward,
            interactRayLength,
            interactLayer,
            QueryTriggerInteraction.Collide
        );

        SetInteractButton(hit);

        // Debug visual di Scene view
        Debug.DrawRay(
            headPoint.position,
            transform.forward * interactRayLength,
            hit ? Color.green : Color.red
        );
    }

    /// <summary>
    /// Dihubungkan ke OnClick Interact Button di Inspector.
    /// </summary>
    public void OnInteractButtonPressed()
    {
        if (currentNearbyShelf == null) return;
        Debug.Log($"[Player] Interact → {currentNearbyShelf.DisplayName}");
        currentNearbyShelf.Interact();
    }

    private void SetInteractButton(bool show)
    {
        if (interactButtonVisible == show) return;
        interactButtonVisible = show;
        interactButton?.SetActive(show);
    }

    // ──────────────────────────────────────────────
    //  RUNTIME SETTINGS (untuk slider nanti)
    // ──────────────────────────────────────────────

    public void SetWalkSpeed(float value) => walkSpeed = value;
    public void SetLookSensitivity(float value) => lookSensitivity = value;
}