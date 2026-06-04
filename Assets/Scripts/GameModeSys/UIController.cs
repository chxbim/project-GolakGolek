// UIController.cs
// Attach ke GameObject "UIController" di bawah Managers (SelectMode DAN MainGameScene).
// TIDAK DontDestroyOnLoad — instance berbeda per scene, tanggung jawab berbeda.
//
// Tanggung jawab di SelectMode:
//   - Dengarkan klik ModeGolek / ModeTimeAttack di PanelMode
//   - Tampilkan child PanelTutorial yang sesuai (ModeGolek atau ModeTimeAttack)
//
// Tanggung jawab di MainGameScene (PlayerCanvas):
//   - Baca GameSession.SelectedMode saat Start()
//   - Toggle TimeAttackTimer (hanya TimeAttack)
//   - Update teks timer realtime via subscribe OnTimerTick
//   - Listing dan BtnPause = always on (kedua mode)
//   - BtnPause: pause full di TimeAttack, UI-only halt di Golek
//
// Yang TIDAK dilakukan script ini:
//   - Menyimpan state game (itu GameModeController)
//   - Navigasi scene (itu SceneController)
//   - Logika menang/kalah (itu GameModeController)

using TMPro;
using Unity.Collections;
using UnityEngine;

public class UIController : MonoBehaviour
{
    // ================================================================
    //  SELECTMODE FIELDS
    //  Isi di Inspector hanya kalau script ini dipakai di SelectMode
    // ================================================================

    [Header("SelectMode — PanelTutorial")]
    [Tooltip("Parent PanelTutorial. Di-set active saat salah satu mode diklik.")]
    [SerializeField] private GameObject panelTutorial;

    [Tooltip("Child PanelTutorial untuk mode Golek. Default: off.")]
    [SerializeField] private GameObject tutorialModeGolek;

    [Tooltip("Child PanelTutorial untuk mode TimeAttack. Default: off.")]
    [SerializeField] private GameObject tutorialModeTimeAttack;

    // ================================================================
    //  MAINGAMESCENE — PLAYERCANVAS FIELDS
    //  Isi di Inspector hanya kalau script ini dipakai di MainGameScene
    // ================================================================

    [Header("MainGameScene — PlayerCanvas (Always On)")]
    [Tooltip("Panel daftar objective item. Aktif di kedua mode.")]
    [SerializeField] private GameObject listing;

    [Tooltip("Tombol pause. Aktif di kedua mode.")]
    [SerializeField] private GameObject btnPause;

    [Header("MainGameScene — PlayerCanvas (TimeAttack Only)")]
    [Tooltip("Panel timer. Hanya aktif di mode TimeAttack.")]
    [SerializeField] private GameObject timeAttackTimer;

    [Tooltip("TMPro text di dalam TimeAttackTimer untuk menampilkan sisa waktu.")]
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("MainGameScene — Pause")]
    [Tooltip("Panel yang muncul saat game di-pause.")]
    [SerializeField] private GameObject panelPause;

    // ================================================================
    //  PRIVATE STATE
    // ================================================================

    private GameModeController _gameModeController;
    private bool _isPaused = false;

    // ================================================================
    //  UNITY LIFECYCLE
    // ================================================================

    private void Start()
    {
        // --- SelectMode setup ---
        // Pastikan PanelTutorial dan semua child-nya off di awal
        if (panelTutorial != null)
        {
            panelTutorial.SetActive(false);
            if (tutorialModeGolek != null) tutorialModeGolek.SetActive(false);
            if (tutorialModeTimeAttack != null) tutorialModeTimeAttack.SetActive(false);
        }

        // --- MainGameScene setup ---
        // Cari GameModeController di scene (hanya ada di MainGameScene)
        _gameModeController = FindObjectOfType<GameModeController>();

        if (_gameModeController != null)
        {
            ApplyModeUI(_gameModeController.CurrentMode);

            // Subscribe timer hanya kalau TimeAttack
            if (_gameModeController.CurrentMode == GameMode.TimeAttack)
            {
                _gameModeController.OnTimerTick += UpdateTimerDisplay;
            }
        }

        // PanelPause default off
        if (panelPause != null) panelPause.SetActive(false);
    }

    private void OnDestroy()
    {
        // Unsubscribe supaya tidak ada memory leak saat scene unload
        if (_gameModeController != null)
        {
            _gameModeController.OnTimerTick -= UpdateTimerDisplay;
        }
    }

    // ================================================================
    //  SELECTMODE — PUBLIC METHODS
    //  Drag UIController GameObject ke OnClick() button di Inspector
    // ================================================================

    // Dipanggil OnClick() ModeGolek di PanelMode
    public void ShowTutorialGolek()
    {
        if (panelTutorial != null) panelTutorial.SetActive(true);
        if (tutorialModeGolek != null) tutorialModeGolek.SetActive(true);
        if (tutorialModeTimeAttack != null) tutorialModeTimeAttack.SetActive(false);
    }

    // Dipanggil OnClick() ModeTimeAttack di PanelMode
    public void ShowTutorialTimeAttack()
    {
        if (panelTutorial != null) panelTutorial.SetActive(true);
        if (tutorialModeTimeAttack != null) tutorialModeTimeAttack.SetActive(true);
        if (tutorialModeGolek != null) tutorialModeGolek.SetActive(false);
    }

    // Dipanggil OnClick() BtnTutup / back button di PanelTutorial
    public void HideTutorial()
    {
        if (panelTutorial != null) panelTutorial.SetActive(false);
        if (tutorialModeGolek != null) tutorialModeGolek.SetActive(false);
        if (tutorialModeTimeAttack != null) tutorialModeTimeAttack.SetActive(false);
    }

    // ================================================================
    //  MAINGAMESCENE — APPLY MODE UI
    // ================================================================

    private void ApplyModeUI(GameMode mode)
    {
        // Always on — kedua mode
        if (listing != null) listing.SetActive(true);
        if (btnPause != null) btnPause.SetActive(true);

        // TimeAttack only
        bool isTA = mode == GameMode.TimeAttack;
        if (timeAttackTimer != null) timeAttackTimer.SetActive(isTA);

        // Inisialisasi teks timer supaya tidak blank sebelum tick pertama
        if (isTA && timerText != null)
            timerText.text = FormatTime(_gameModeController != null
                ? _gameModeController.TimeRemaining
                : 0f);
    }

    // ================================================================
    //  TIMER DISPLAY
    // ================================================================

    // Dipanggil setiap frame via GameModeController.OnTimerTick
    private void UpdateTimerDisplay(float secondsRemaining)
    {
        if (timerText != null)
            timerText.text = FormatTime(secondsRemaining);
    }

    // Format detik → "MM:SS"
    private string FormatTime(float seconds)
    {
        int m = Mathf.FloorToInt(seconds / 60f);
        int s = Mathf.FloorToInt(seconds % 60f);
        return $"{m:00}:{s:00}";
    }

    // ================================================================
    //  PAUSE
    //  Dipanggil OnClick() BtnPause
    // ================================================================

    public void TogglePause()
    {
        _isPaused = !_isPaused;

        if (_gameModeController != null &&
            _gameModeController.CurrentMode == GameMode.TimeAttack)
        {
            // TimeAttack: pause penuh — hentikan semua gameplay lewat Time.timeScale
            // Timer coroutine di GameModeController otomatis berhenti karena Time.deltaTime = 0
            Time.timeScale = _isPaused ? 0f : 1f;
        }
        // Golek: tidak ada Time.timeScale — pause cuma tampil UI saja (player halt visual)

        if (panelPause != null) panelPause.SetActive(_isPaused);
    }

    // Dipanggil OnClick() BtnLanjut di dalam PanelPause
    public void ResumeGame()
    {
        _isPaused = false;
        Time.timeScale = 1f;            // safe to reset untuk kedua mode
        if (panelPause != null) panelPause.SetActive(false);
    }

    public void ListingItem()
    {
        //manggil dri ListingBarang.cs
        //-tentuin GameMode: Golek
        //-fungsi randomize dari ListingBarang
        // ListingBarang.randomize.Golek
        //-tampilin ke ListingUI
        //    -List1apa 
        //    -List2apa
    }
}