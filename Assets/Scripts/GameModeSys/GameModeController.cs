// GameModeController.cs
// Attach ke GameObject "GameModeController" di MainGameScene.
// TIDAK DontDestroyOnLoad — hanya hidup di MainGameScene.
//
// Tanggung jawab:
//   - Baca GameSession.SelectedMode saat scene load
//   - Jalankan countdown timer (hanya TimeAttack)
//   - Deteksi win condition via CartSystem.OnCartChanged
//   - Fire events ke UIController (dan siapapun yang subscribe)
//
// Yang TIDAK dilakukan script ini:
//   - Update UI langsung (itu urusan UIController)
//   - POST ke API (itu urusan CartSystem + APIManager)
//   - Navigasi scene (itu urusan SceneController)

using System;
using System.Collections;
using UnityEngine;

public class GameModeController : MonoBehaviour
{
    // ----------------------------------------------------------------
    //  Inspector Fields
    // ----------------------------------------------------------------

    [Header("Time Attack Settings")]
    [Tooltip("Durasi waktu mode Time Attack dalam detik.")]
    [SerializeField] private float timeAttackDuration = 120f;   // 2 menit default

    [Header("Target Items")]
    [Tooltip("Jumlah item yang harus dikumpulkan player untuk menang.")]
    [SerializeField] private int targetItemCount = 8;

    // ----------------------------------------------------------------
    //  Public Read-Only State
    // ----------------------------------------------------------------

    public GameMode CurrentMode { get; private set; }
    public float TimeRemaining { get; private set; }
    public bool IsGameOver { get; private set; }
    public bool IsGameWon { get; private set; }

    // Convenience: apakah timer sedang berjalan
    public bool IsTimerRunning => CurrentMode == GameMode.time_attack
                               && !IsGameOver;

    // ----------------------------------------------------------------
    //  Events — UIController PlayerCanvas MainGameScene subscribe ke sini
    // ----------------------------------------------------------------

    // Dipanggil setiap frame saat timer aktif; kirim sisa waktu dalam detik
    public event Action<float> OnTimerTick;

    // Dipanggil sekali saat timer habis (khusus TimeAttack)
    public event Action OnTimerExpired;

    // Dipanggil saat player berhasil kumpulkan semua target item
    public event Action OnGameWin;

    // Dipanggil saat game over (kalah — waktu habis di TimeAttack)
    public event Action OnGameLose;

    // Dipanggil setiap kali jumlah item di cart berubah; kirim count terbaru
    public event Action<int> OnCartCountChanged;

    // ----------------------------------------------------------------
    //  Private
    // ----------------------------------------------------------------

    private Coroutine _timerCoroutine;

    // ----------------------------------------------------------------
    //  Unity Lifecycle
    // ----------------------------------------------------------------

    private void Awake()
    {
        CurrentMode = GameSession.SelectedMode;
        IsGameOver = false;
        IsGameWon = false;
        TimeRemaining = timeAttackDuration;
    }

    private void Start()
    {
        // Subscribe ke CartSystem supaya bisa cek win condition
        // setiap kali player ambil item
        if (CartSystem.Instance != null)
        {
            CartSystem.Instance.OnCartChanged += HandleCartChanged;
        }
        else
        {
            Debug.LogWarning("[GameModeController] CartSystem.Instance null di Start(). " +
                             "Pastikan CartSystem ada di scene atau DontDestroyOnLoad aktif.");
        }

        // Mulai timer hanya kalau mode TimeAttack
        if (CurrentMode == GameMode.time_attack)
        {
            _timerCoroutine = StartCoroutine(TimerCoroutine());
        }

        Debug.Log($"[GameModeController] Mode aktif: {CurrentMode}");
    }

    private void OnDestroy()
    {
        // Unsubscribe supaya tidak ada memory leak
        if (CartSystem.Instance != null)
        {
            CartSystem.Instance.OnCartChanged -= HandleCartChanged;
        }
    }

    // ----------------------------------------------------------------
    //  Timer (hanya TimeAttack)
    // ----------------------------------------------------------------

    private IEnumerator TimerCoroutine()
    {
        while (TimeRemaining > 0f && !IsGameOver)
        {
            yield return null;                          // tunggu 1 frame
            TimeRemaining -= Time.deltaTime;
            TimeRemaining = Mathf.Max(TimeRemaining, 0f);

            OnTimerTick?.Invoke(TimeRemaining);
        }

        // Coroutine selesai karena waktu habis (bukan karena IsGameOver di-set duluan)
        if (!IsGameOver && !IsGameWon)
        {
            TriggerGameLose();
        }
    }

    // ----------------------------------------------------------------
    //  Win / Lose Logic
    // ----------------------------------------------------------------

    // Dipanggil CartSystem.OnCartChanged
    private void HandleCartChanged()
    {
        if (IsGameOver) return;

        int currentCount = CartSystem.Instance.GetEntries().Count;
        OnCartCountChanged?.Invoke(currentCount);

        CheckWinCondition(currentCount);
    }

    // Public supaya bisa dipanggil manual dari luar kalau perlu (misal: debug, kasir trigger)
    public void CheckWinCondition(int cartCount)
    {
        if (IsGameOver) return;

        if (cartCount >= targetItemCount)
        {
            TriggerGameWin();
        }
    }

    private void TriggerGameWin()
    {
        IsGameOver = true;
        IsGameWon = true;

        StopTimerIfRunning();

        Debug.Log($"[GameModeController] Game WIN — mode: {CurrentMode}, " +
                  $"waktu: {GetElapsedTime():F1}s");

        OnGameWin?.Invoke();
    }

    private void TriggerGameLose()
    {
        IsGameOver = true;
        IsGameWon = false;

        Debug.Log("[GameModeController] Game LOSE — waktu habis.");

        OnTimerExpired?.Invoke();
        OnGameLose?.Invoke();
    }

    // ----------------------------------------------------------------
    //  Public Helpers
    // ----------------------------------------------------------------

    // Berapa detik yang sudah berlalu sejak game mulai
    public float GetElapsedTime()
    {
        return timeAttackDuration - TimeRemaining;
    }

    // Kembalikan mode sebagai string sesuai format POST backend
    // ("TimeAttack" atau "Golek")
    public string GetModeString()
    {
        return CurrentMode == GameMode.time_attack ? "time_attack" : "golek";
    }

    // Kembalikan waktuSelesai untuk CartPayload:
    //   TimeAttack → elapsed time dalam detik
    //   Golek      → 0 (tidak ada timer)
    public float GetWaktuSelesai()
    {
        return CurrentMode == GameMode.time_attack ? GetElapsedTime() : 0f;
    }

    // ----------------------------------------------------------------
    //  Private Helpers
    // ----------------------------------------------------------------

    private void StopTimerIfRunning()
    {
        if (_timerCoroutine != null)
        {
            StopCoroutine(_timerCoroutine);
            _timerCoroutine = null;
        }
    }
}