using System.Collections;
using UnityEngine;

/// <summary>
/// Attach ke CashierUI (PlayerCanvas) — HANYA SATU instance di scene.
/// OnTriggerEnter/Exit TIDAK ditangani di sini — delegasi ke CashierTriggerZone.cs
/// yang attach di isTrigger Cashier 3D Env.
///
/// Inspector wiring:
///   btnSubmitKasir  → BtnSubmit (child AreaKasir), default inactive
///   areaCashierUI   → AreaCashier image 2D UI, default inactive (muncul bareng BtnSubmit)
///   gameConditionUI → GameConditionUI parent
///   gameKalah       → GameConditionUI/gameKalah
///   gameMenang      → GameConditionUI/gameMenang
///   gameIncomplete  → GameConditionUI/gameIncomplete
///   sceneLoader     → SceneLoader di scene
///   joystick        → Joystick GameObject di PlayerCanvas (untuk freeze saat game over)
///   btnPause        → BtnPause di PlayerCanvas (disable saat game over)
///
///   BtnSubmit.OnClick                → CashierController.OnSubmitKasirPressed()
///   BtnBack (gameKalah).OnClick      → CashierController.PostTimerExpiredAndReturn()
///   BtnBack (gameMenang).OnClick     → SceneLoader.LoadSelectMode()
///   BtnBack (gameIncomplete).OnClick → SceneLoader.LoadSelectMode()
/// </summary>
public class CashierController : MonoBehaviour
{
    // ── Inspector ────────────────────────────────────────────

    [Header("Kasir UI — muncul saat player di zona kasir")]
    [Tooltip("BtnSubmit di CashierUI/AreaKasir. Default inactive.")]
    public GameObject btnSubmitKasir;
    [Tooltip("Image 2D AreaCashier di CashierUI. Muncul bersamaan dengan BtnSubmit.")]
    public GameObject AreaKasir;

    [Header("GameConditionUI — Parent")]
    [Tooltip("Parent container semua panel kondisi. Diaktifkan sebelum child-nya.")]
    public GameObject gameConditionUI;

    [Header("GameConditionUI — Child Panels")]
    public GameObject gameKalah;
    public GameObject gameMenang;
    public GameObject gameIncomplete;

    [Header("Scene Navigation")]
    public SceneLoader sceneLoader;

    [Header("Player Input — freeze saat game over")]
    [Tooltip("Joystick di PlayerCanvas. Di-nonaktifkan saat GameConditionUI muncul.")]
    public GameObject joystick;
    [Tooltip("BtnPause di PlayerCanvas. Di-nonaktifkan saat GameConditionUI muncul.")]
    public GameObject btnPause;

    // ── Private refs ─────────────────────────────────────────

    private GameModeController _gameModeController;
    private ListingBarang _listingBarang;

    private bool _sudahPost = false;
    private CartPayload _pendingTimerExpiredPayload = null;

    // ── Lifecycle ─────────────────────────────────────────────

    private void Awake()
    {
        _gameModeController = FindFirstObjectByType<GameModeController>();
        _listingBarang = FindFirstObjectByType<ListingBarang>();

        if (_gameModeController == null)
            Debug.LogError("[Kasir] GameModeController tidak ditemukan di scene!");
        if (_listingBarang == null)
            Debug.LogError("[Kasir] ListingBarang tidak ditemukan di scene!");
        if (btnSubmitKasir == null)
            Debug.LogError("[Kasir] btnSubmitKasir belum di-assign di Inspector!");
        if (gameConditionUI == null)
            Debug.LogError("[Kasir] gameConditionUI belum di-assign di Inspector!");
    }

    private void Start()
    {
        // Sembunyikan semua UI kasir di awal
        SetActive(btnSubmitKasir, false);
        SetActive(AreaKasir, false);
        SetActive(gameConditionUI, false);
        SetActive(gameKalah, false);
        SetActive(gameMenang, false);
        SetActive(gameIncomplete, false);

        if (_gameModeController != null)
        {
            _gameModeController.OnTimerExpired += HandleTimerExpired;
            Debug.Log("[Kasir] Subscribe ke OnTimerExpired.");
        }
    }

    private void OnDestroy()
    {
        if (_gameModeController != null)
            _gameModeController.OnTimerExpired -= HandleTimerExpired;
    }

    // ── Dipanggil CashierTriggerZone ─────────────────────────

    /// <summary>Dipanggil CashierTriggerZone saat player masuk isTrigger kasir.</summary>
    public void OnPlayerEnterKasir()
    {
        if (_sudahPost) return;
        Debug.Log("[Kasir] Player masuk zona kasir.");
        SetActive(btnSubmitKasir, true);
        SetActive(AreaKasir, true);
    }

    /// <summary>Dipanggil CashierTriggerZone saat player keluar isTrigger kasir.</summary>
    public void OnPlayerExitKasir()
    {
        Debug.Log("[Kasir] Player keluar zona kasir.");
        SetActive(btnSubmitKasir, false);
        SetActive(AreaKasir, false);
    }

    // ── BtnSubmit.OnClick ─────────────────────────────────────

    public void OnSubmitKasirPressed()
    {
        if (_sudahPost)
        {
            Debug.LogWarning("[Kasir] Submit diabaikan — sudah POST sebelumnya.");
            return;
        }

        if (CartSystem.Instance == null)
        {
            Debug.LogError("[Kasir] CartSystem.Instance null!");
            return;
        }

        bool semuaTerkumpul = CartSystem.Instance.GetItemCount() >= _listingBarang.jumlahItemPerSesi;
        bool isGolek = _gameModeController != null
                    && _gameModeController.CurrentMode == GameMode.golek;

        Debug.Log($"[Kasir] Submit | SemuaTerkumpul={semuaTerkumpul} | Mode={_gameModeController?.CurrentMode}");

        if (isGolek && !semuaTerkumpul)
        {
            Debug.Log("[Kasir] Golek incomplete — tidak POST.");
            SetActive(btnSubmitKasir, false);
            SetActive(AreaKasir, false);
            ShowPanel(gameIncomplete);
            return;
        }

        string outcome = semuaTerkumpul ? "menang" : "kalah_incomplete_ta";
        StartCoroutine(PostAndShowResult(outcome));
    }

    // ── OnTimerExpired handler ────────────────────────────────

    private void HandleTimerExpired()
    {
        if (_sudahPost) return;

        Debug.Log("[Kasir] Timer expired — payload disiapkan.");

        SetActive(btnSubmitKasir, false);
        SetActive(AreaKasir, false);

        GameCashierData sessionData = BuildSessionData(0f);
        _pendingTimerExpiredPayload = CartSystem.Instance.BuildPayload(
            sessionData, _listingBarang.jumlahItemPerSesi);

        Debug.Log($"[Kasir] Payload kalah | item_ditemukan={_pendingTimerExpiredPayload.item_ditemukan} | koin={_pendingTimerExpiredPayload.koin}");

        ShowPanel(gameKalah);
    }

    // ── BtnBack (gameKalah).OnClick ───────────────────────────

    public void PostTimerExpiredAndReturn()
    {
        if (_pendingTimerExpiredPayload == null)
        {
            Debug.LogWarning("[Kasir] payload null — langsung navigate.");
            sceneLoader?.LoadSelectMode();
            return;
        }

        StartCoroutine(PostPayloadAndNavigate(_pendingTimerExpiredPayload));
    }

    // ── Coroutines ───────────────────────────────────────────

    private IEnumerator PostAndShowResult(string outcome)
    {
        _sudahPost = true;
        SetActive(btnSubmitKasir, false);
        SetActive(AreaKasir, false);

        float waktuSelesai = _gameModeController != null
            ? _gameModeController.GetWaktuSelesai() : 0f;

        GameCashierData sessionData = BuildSessionData(waktuSelesai);
        CartPayload payload = CartSystem.Instance.BuildPayload(
            sessionData, _listingBarang.jumlahItemPerSesi);

        Debug.Log($"[Kasir] POST | outcome={outcome} | player_id={payload.player_id} | " +
                  $"mode={payload.mode} | item_ditemukan={payload.item_ditemukan} | " +
                  $"total_harga={payload.total_harga} | koin={payload.koin} | " +
                  $"waktu_selesai={payload.waktu_selesai} | timestamp={payload.timestamp}");

        bool postSelesai = false;
        APIManager.Instance.PostCart(payload,
            onSuccess: r => { Debug.Log($"[Kasir] POST berhasil | {r}"); postSelesai = true; },
            onError: e => { Debug.LogError($"[Kasir] POST gagal: {e}"); postSelesai = true; }
        );

        float elapsed = 0f;
        while (!postSelesai && elapsed < 10f)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!postSelesai)
            Debug.LogWarning("[Kasir] POST timeout — lanjut tampil UI.");

        ShowPanel(outcome == "menang" ? gameMenang : gameKalah);
    }

    private IEnumerator PostPayloadAndNavigate(CartPayload payload)
    {
        _sudahPost = true;

        Debug.Log($"[Kasir] POST kalah | player_id={payload.player_id} | " +
                  $"mode={payload.mode} | item_ditemukan={payload.item_ditemukan} | " +
                  $"koin={payload.koin} | waktu_selesai={payload.waktu_selesai}");

        bool postSelesai = false;
        APIManager.Instance.PostCart(payload,
            onSuccess: r => { Debug.Log($"[Kasir] POST kalah berhasil | {r}"); postSelesai = true; },
            onError: e => { Debug.LogError($"[Kasir] POST kalah gagal: {e}"); postSelesai = true; }
        );

        float elapsed = 0f;
        while (!postSelesai && elapsed < 10f)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!postSelesai)
            Debug.LogWarning("[Kasir] POST timeout — navigate tetap jalan.");

        sceneLoader?.LoadSelectMode();
    }

    // ── Helpers ──────────────────────────────────────────────

    private GameCashierData BuildSessionData(float waktuSelesai)
    {
        return new GameCashierData
        {
            playerId = SessionCache.UserId,
            mode = _gameModeController?.GetModeString() ?? "Unknown",
            waktuSelesai = waktuSelesai,
            itemDitemukan = CartSystem.Instance.GetItemCount(),
            timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };
    }

    /// <summary>
    /// Aktifkan GameConditionUI parent, matikan semua child panel,
    /// aktifkan panel yang diminta, lalu freeze input player.
    /// </summary>
    private void ShowPanel(GameObject panel)
    {
        if (panel == null)
        {
            Debug.LogError("[Kasir] ShowPanel: panel null!");
            return;
        }

        // Aktifkan parent dulu supaya child visible
        SetActive(gameConditionUI, true);

        // Reset semua child, aktifkan yang diminta
        SetActive(gameKalah, false);
        SetActive(gameMenang, false);
        SetActive(gameIncomplete, false);
        SetActive(panel, true);

        // Freeze player input — sama seperti PausePanelUI
        SetActive(joystick, false);

        Debug.Log($"[Kasir] ShowPanel → {panel.name} | input player difreeze.");
    }

    private static void SetActive(GameObject obj, bool active)
    {
        if (obj != null) obj.SetActive(active);
    }
}