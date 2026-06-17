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
using UnityEngine;

public class UIController : MonoBehaviour
{
    // ================================================================
    //  SELECTMODE FIELDS
    // ================================================================

    [Header("SelectMode — PanelTutorial")]
    [SerializeField] private GameObject panelTutorial;
    [SerializeField] private GameObject tutorialModeGolek;
    [SerializeField] private GameObject tutorialModeTimeAttack;

    // ================================================================
    //  MAINGAMESCENE — PLAYERCANVAS FIELDS
    // ================================================================

    [Header("MainGameScene — PlayerCanvas (Always On)")]
    [SerializeField] private GameObject listing;
    [SerializeField] private GameObject btnPause;

    [Header("MainGameScene — PlayerCanvas (TimeAttack Only)")]
    [SerializeField] private GameObject timeAttackTimer;
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("MainGameScene — Pause")]
    [SerializeField] private GameObject panelPause;

    // ================================================================
    //  PRIVATE STATE
    // ================================================================

    private GameModeController _gameModeController;
    private bool _isPaused = false;
    private bool _listingVisible = true;   // listing default on saat game mulai

    // ================================================================
    //  UNITY LIFECYCLE
    // ================================================================

    private void Start()
    {

        Debug.Log($"BtnPause active: {btnPause.activeSelf}, inHierarchy: {btnPause.activeInHierarchy}");

        // --- SelectMode setup ---
        if (panelTutorial != null)
        {
            panelTutorial.SetActive(false);
            if (tutorialModeGolek != null) tutorialModeGolek.SetActive(false);
            if (tutorialModeTimeAttack != null) tutorialModeTimeAttack.SetActive(false);
        }

        // --- MainGameScene setup ---
        _gameModeController = FindObjectOfType<GameModeController>();

        if (_gameModeController != null)
        {
            ApplyModeUI(_gameModeController.CurrentMode);

            if (_gameModeController.CurrentMode == GameMode.time_attack)
                _gameModeController.OnTimerTick += UpdateTimerDisplay;
        }

        if (panelPause != null) panelPause.SetActive(false);
        Debug.Log($"BtnPause active: {btnPause.activeSelf}, inHierarchy: {btnPause.activeInHierarchy}");
    }

    private void OnDestroy()
    {
        if (_gameModeController != null)
            _gameModeController.OnTimerTick -= UpdateTimerDisplay;
    }

    // ================================================================
    //  SELECTMODE — PUBLIC METHODS
    // ================================================================

    public void ShowTutorialGolek()
    {
        if (panelTutorial != null) panelTutorial.SetActive(true);
        if (tutorialModeGolek != null) tutorialModeGolek.SetActive(true);
        if (tutorialModeTimeAttack != null) tutorialModeTimeAttack.SetActive(false);
    }

    public void ShowTutorialTimeAttack()
    {
        if (panelTutorial != null) panelTutorial.SetActive(true);
        if (tutorialModeTimeAttack != null) tutorialModeTimeAttack.SetActive(true);
        if (tutorialModeGolek != null) tutorialModeGolek.SetActive(false);
    }

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
        if (listing != null) listing.SetActive(true);
        if (btnPause != null) btnPause.SetActive(true);

        bool isTA = mode == GameMode.time_attack;
        if (timeAttackTimer != null) timeAttackTimer.SetActive(isTA);

        if (isTA && timerText != null)
            timerText.text = FormatTime(_gameModeController != null
                ? _gameModeController.TimeRemaining : 0f);
    }

    // ================================================================
    //  LISTING TOGGLE
    //  Hubungkan ke OnClick() BtnListing di Inspector.
    // ================================================================

    /// <summary>
    /// Toggle visibilitas panel listing belanja.
    /// Dipanggil OnClick() button listing di PlayerCanvas.
    /// </summary>
    public void ListingItem()
    {
        if (listing == null) return;

        _listingVisible = !_listingVisible;
        listing.SetActive(_listingVisible);

        Debug.Log($"[UI] Listing {(_listingVisible ? "ditampilkan" : "disembunyikan")}");
    }

    // ================================================================
    //  TIMER DISPLAY
    // ================================================================

    private void UpdateTimerDisplay(float secondsRemaining)
    {
        if (timerText != null)
            timerText.text = FormatTime(secondsRemaining);
    }

    private string FormatTime(float seconds)
    {
        int m = Mathf.FloorToInt(seconds / 60f);
        int s = Mathf.FloorToInt(seconds % 60f);
        return $"{m:00}:{s:00}";
    }

    // ================================================================
    //  PAUSE
    // ================================================================

    public void TogglePause()
    {

        _isPaused = !_isPaused;
        Debug.Log($"_isPaused: {_isPaused}");

        if (_gameModeController != null &&
            _gameModeController.CurrentMode == GameMode.time_attack) 
        {
            Time.timeScale = _isPaused ? 0f : 1f;
        }

        if (panelPause != null) panelPause.SetActive(_isPaused);
    }


    public void ResumeGame()
    {
        _isPaused = false;
        Time.timeScale = 1f;
        if (panelPause != null) panelPause.SetActive(false);
    }
}