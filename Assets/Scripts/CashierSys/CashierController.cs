using System.Collections;
using UnityEngine;

/// <summary>
/// Attach ke CashierUI — HANYA SATU instance di scene.
///
/// FLOW PER KONDISI:
///
///   [TimeAttack — Menang]
///     Player masuk kasir setelah semua item terkumpul → BtnSubmit muncul
///     → tap → PostAndShowResult("menang") → POST (koin=25) → gameMenang
///     → BtnBack gameMenang → SceneLoader.LoadSelectMode() langsung
///
///   [TimeAttack — Kalah (waktu habis)]
///     OnTimerExpired → HandleTimerExpired()
///     → build + simpan payload (waktu_selesai=0f) → tampil gameKalah
///     → BtnBack gameKalah → PostTimerExpiredAndReturn() → POST → LoadSelectMode
///
///   [TimeAttack — Incomplete via kasir]
///     Player masuk kasir sebelum semua item → BtnSubmit muncul
///     → tap → PostAndShowResult("kalah_incomplete_ta") → POST (koin=0) → gameKalah
///     → BtnBack gameKalah → PostTimerExpiredAndReturn()
///       (payload sudah null di skenario ini, skip POST, langsung LoadSelectMode)
///
///   [Golek — Menang]
///     Player masuk kasir setelah semua item → BtnSubmit muncul
///     → tap → PostAndShowResult("menang") → POST (koin=25) → gameMenang
///     → BtnBack gameMenang → SceneLoader.LoadSelectMode() langsung
///
///   [Golek — Incomplete via kasir]
///     Player masuk kasir sebelum semua item → BtnSubmit muncul
///     → tap → TIDAK POST → tampil gameIncomplete
///     → BtnBack gameIncomplete → SceneLoader.LoadSelectMode() langsung
///
/// INSPECTOR WIRING:
///   btnSubmitKasir      → BtnSubmit (child AreaKasir), default inactive di scene
///   gameConditionUI     → GameConditionUI (parent semua panel kondisi)
///   gameKalah           → GameConditionUI/gameKalah
///   gameMenang          → GameConditionUI/gameMenang
///   gameIncomplete      → GameConditionUI/gameIncomplete
///   sceneLoader         → SceneLoader di scene
///
///   BtnSubmit.OnClick           → CashierController.OnSubmitKasirPressed()
///   BtnBack (gameKalah).OnClick → CashierController.PostTimerExpiredAndReturn()
///   BtnBack (gameMenang).OnClick         → SceneLoader.LoadSelectMode()
///   BtnBack (gameIncomplete).OnClick     → SceneLoader.LoadSelectMode()
/// </summary>
public class CashierController : MonoBehaviour
{
    // ── Inspector ────────────────────────────────────────────

    [Header("Kasir Submit Button")]
    [Tooltip("BtnSubmit di CashierUI/AreaKasir. Default inactive.")]
    public GameObject btnSubmitKasir;

    [Header("GameConditionUI — Parent")]
    [Tooltip("Parent object yang menampung semua panel kondisi. Wajib diaktifkan sebelum child-nya.")]
    public GameObject gameConditionUI;

    [Header("GameConditionUI — Child Panels")]
    public GameObject gameKalah;
    public GameObject gameMenang;
    public GameObject gameIncomplete;

    [Header("Scene Navigation")]
    public SceneLoader sceneLoader;

    // ── Private refs ─────────────────────────────────────────

    private GameModeController _gameModeController;
    private ListingBarang _listingBarang;

    // Guard: POST hanya boleh terjadi sekali per sesi
    private bool _sudahPost = false;

    // Payload kalah (timer expired) disiapkan saat timer habis,
    // dikirim nanti saat player tap BtnBack
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
        // Sembunyikan tombol submit di awal
        SetActive(btnSubmitKasir, false);

        // Matikan seluruh GameConditionUI beserta semua child panel-nya
        SetActive(gameConditionUI, false);
        SetActive(gameKalah, false);
        SetActive(gameMenang, false);
        SetActive(gameIncomplete, false);

        // Subscribe ke event timer habis
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

    // ── Trigger Zone ─────────────────────────────────────────

    public void OnPlayerEnterKasir()
    {
        if (_sudahPost) return;
        Debug.Log("[Kasir] Player masuk zona kasir — BtnSubmit aktif.");
        SetActive(btnSubmitKasir, true);
    }

    public void OnPlayerExitKasir()
    {
        Debug.Log("[Kasir] Player keluar zona kasir — BtnSubmit nonaktif.");
        SetActive(btnSubmitKasir, false);
    }

    // ── Public: BtnSubmit.OnClick ─────────────────────────────

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
                    && _gameModeController.CurrentMode == GameMode.Golek;

        Debug.Log($"[Kasir] Submit ditekan | SemuaTerkumpul={semuaTerkumpul} | Mode={_gameModeController?.CurrentMode}");

        // Golek + belum semua → incomplete, TIDAK POST
        if (isGolek && !semuaTerkumpul)
        {
            Debug.Log("[Kasir] Golek incomplete — tidak POST.");
            SetActive(btnSubmitKasir, false);
            ShowPanel(gameIncomplete);
            return;
        }

        // Semua kondisi lain → POST
        string outcome = semuaTerkumpul ? "menang" : "kalah_incomplete_ta";
        StartCoroutine(PostAndShowResult(outcome));
    }

    // ── Event Handler: timer habis (TimeAttack only) ──────────

    private void HandleTimerExpired()
    {
        if (_sudahPost) return;

        Debug.Log("[Kasir] Timer expired — payload disiapkan, menunggu BtnBack.");

        SetActive(btnSubmitKasir, false);

        GameCashierData sessionData = BuildSessionData(waktuSelesaiOverride: 0f);
        _pendingTimerExpiredPayload = CartSystem.Instance.BuildPayload(
            sessionData,
            _listingBarang.jumlahItemPerSesi
        );

        Debug.Log($"[Kasir] Payload kalah siap | item_ditemukan={_pendingTimerExpiredPayload.item_ditemukan} | koin={_pendingTimerExpiredPayload.koin}");

        ShowPanel(gameKalah);
    }

    // ── Public: BtnBack (gameKalah).OnClick ──────────────────

    public void PostTimerExpiredAndReturn()
    {
        if (_pendingTimerExpiredPayload == null)
        {
            // Skenario incomplete TA: panel gameKalah muncul tapi via PostAndShowResult,
            // bukan via HandleTimerExpired — payload tidak ada, langsung navigate.
            Debug.LogWarning("[Kasir] PostTimerExpiredAndReturn: payload null — langsung navigate.");
            sceneLoader?.LoadSelectMode();
            return;
        }

        StartCoroutine(PostPayloadAndNavigate(_pendingTimerExpiredPayload));
    }

    // ── Coroutines ───────────────────────────────────────────

    /// <summary>POST dari submit kasir (menang / kalah incomplete TA).</summary>
    private IEnumerator PostAndShowResult(string outcome)
    {
        _sudahPost = true;
        SetActive(btnSubmitKasir, false);

        float waktuSelesai = _gameModeController != null
            ? _gameModeController.GetWaktuSelesai()
            : 0f;

        GameCashierData sessionData = BuildSessionData(waktuSelesaiOverride: waktuSelesai);
        CartPayload payload = CartSystem.Instance.BuildPayload(
            sessionData,
            _listingBarang.jumlahItemPerSesi
        );

        Debug.Log($"[Kasir] POST dimulai | outcome={outcome} | player_id={payload.player_id} | " +
                  $"mode={payload.mode} | item_ditemukan={payload.item_ditemukan} | " +
                  $"total_harga={payload.total_harga} | koin={payload.koin} | " +
                  $"waktu_selesai={payload.waktu_selesai} | timestamp={payload.timestamp}");

        bool postSelesai = false;

        APIManager.Instance.PostCart(payload,
            onSuccess: (response) =>
            {
                Debug.Log($"[Kasir] POST berhasil | response={response}");
                postSelesai = true;
            },
            onError: (error) =>
            {
                Debug.LogError($"[Kasir] POST gagal: {error}");
                postSelesai = true;
            }
        );

        float elapsed = 0f;
        while (!postSelesai && elapsed < 10f)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!postSelesai)
            Debug.LogWarning("[Kasir] POST timeout 10 detik — lanjut tampil UI.");

        if (outcome == "menang")
            ShowPanel(gameMenang);
        else
            ShowPanel(gameKalah);
    }

    /// <summary>POST payload yang sudah disiapkan (timer expired), lalu navigate.</summary>
    private IEnumerator PostPayloadAndNavigate(CartPayload payload)
    {
        _sudahPost = true;

        Debug.Log($"[Kasir] POST kalah timer expired | player_id={payload.player_id} | " +
                  $"mode={payload.mode} | item_ditemukan={payload.item_ditemukan} | " +
                  $"total_harga={payload.total_harga} | koin={payload.koin} | " +
                  $"waktu_selesai={payload.waktu_selesai} | timestamp={payload.timestamp}");

        bool postSelesai = false;

        APIManager.Instance.PostCart(payload,
            onSuccess: (response) =>
            {
                Debug.Log($"[Kasir] POST kalah berhasil | response={response}");
                postSelesai = true;
            },
            onError: (error) =>
            {
                Debug.LogError($"[Kasir] POST kalah gagal: {error} — tetap navigate.");
                postSelesai = true;
            }
        );

        float elapsed = 0f;
        while (!postSelesai && elapsed < 10f)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!postSelesai)
            Debug.LogWarning("[Kasir] POST timeout — navigate tetap dijalankan.");

        sceneLoader?.LoadSelectMode();
    }

    // ── Helpers ──────────────────────────────────────────────

    private GameCashierData BuildSessionData(float waktuSelesaiOverride)
    {
        return new GameCashierData
        {
            playerId = SessionCache.UserId,
            mode = _gameModeController != null
                                ? _gameModeController.GetModeString()
                                : "Unknown",
            waktuSelesai = waktuSelesaiOverride,
            itemDitemukan = CartSystem.Instance.GetItemCount(),
            timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };
    }

    /// <summary>
    /// Aktifkan parent GameConditionUI dulu, lalu aktifkan panel yang dituju.
    /// Sembunyikan panel lain yang tidak relevan supaya tidak overlap.
    /// </summary>
    private void ShowPanel(GameObject panel)
    {
        if (panel == null)
        {
            Debug.LogError("[Kasir] ShowPanel: panel null!");
            return;
        }

        // Aktifkan parent container dulu
        SetActive(gameConditionUI, true);

        // Matikan semua child panel, baru aktifkan yang diminta
        SetActive(gameKalah, false);
        SetActive(gameMenang, false);
        SetActive(gameIncomplete, false);
        SetActive(panel, true);

        Debug.Log($"[Kasir] ShowPanel → {panel.name}");
    }

    private static void SetActive(GameObject obj, bool active)
    {
        if (obj != null) obj.SetActive(active);
    }
}