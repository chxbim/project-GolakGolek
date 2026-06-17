using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// MusicController
/// ----------------
/// Mengatur musik latar untuk seluruh game GolakGolek.
///
/// - Musik Menu diputar di semua Scene (HomeScene, LoginScene, SelectMode, ProfileScene, dst).
/// - Musik Game diputar khusus saat berada di MainGameScene.
/// - Transisi antar musik menggunakan fade out → fade in selama 1 detik.
/// - Mute bersifat global (AudioListener.volume), per-session (tidak disimpan ke PlayerPrefs).
///
/// Pasang script ini di sebuah GameObject persist (mis. "MusicController") di scene
/// pertama yang di-load (HomeScene). Object ini akan DontDestroyOnLoad sehingga hanya
/// butuh 1 instance sepanjang hidup aplikasi — sama seperti pola CartSystem / APIManager.
///
/// UI tinggal panggil:
///   MusicController.Instance.ToggleMute();
///   MusicController.Instance.SetMute(true/false);
///   MusicController.Instance.IsMuted
///   MusicController.Instance.OnMuteChanged += (isMuted) => { ... update icon ... };
/// </summary>
[DisallowMultipleComponent]
public class MusicController : MonoBehaviour
{
    public static MusicController Instance { get; private set; }

    [Header("Audio Sources")]
    [Tooltip("AudioSource untuk Musik Menu (musik default, semua scene selain MainGameScene).")]
    [SerializeField] private AudioSource sourceMusicMenu;

    [Tooltip("AudioSource untuk Musik Game (khusus MainGameScene).")]
    [SerializeField] private AudioSource sourceMusicGame;

    [Header("Clips")]
    [SerializeField] private AudioClip musicMenuClip;
    [SerializeField] private AudioClip musicGameClip;

    [Header("Scene Rules")]
    [Tooltip("Nama scene yang memutar Musik Game. Scene lain otomatis memutar Musik Menu.")]
    [SerializeField] private string mainGameSceneName = "MainGameScene";

    [Header("Fade Settings")]
    [Tooltip("Durasi fade out + fade in saat pindah musik (detik).")]
    [SerializeField] private float fadeDuration = 1f;

    [Header("Volume")]
    [Range(0f, 1f)][SerializeField] private float musicMenuVolume = 1f;
    [Range(0f, 1f)][SerializeField] private float musicGameVolume = 1f;

    [Header("Volume Button UI (drag manual per-scene)")]
    [Tooltip("GameObject tombol yang tampil saat audio TIDAK di-mute (volume on).")]
    [SerializeField] private GameObject btnVolumeOn;

    [Tooltip("GameObject tombol yang tampil saat audio di-mute.")]
    [SerializeField] private GameObject btnVolumeMute;

    /// <summary>True kalau audio sedang di-mute (global, lewat AudioListener.volume).</summary>
    public bool IsMuted { get; private set; }

    /// <summary>Event untuk UI. Subscribe ini untuk update icon/toggle mute button.</summary>
    public event Action<bool> OnMuteChanged;

    private enum MusicTrack { None, Menu, Game }
    private MusicTrack currentTrack = MusicTrack.None;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureAudioSources();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void Start()
    {
        // Scene pertama (mis. HomeScene) tidak memicu sceneLoaded karena sudah aktif
        // sebelum MusicController.Awake() berjalan, jadi kita tentukan musik awal manual.
        PlayForScene(SceneManager.GetActiveScene().name, instant: true);
        RefreshVolumeButtonUI();
    }

    private void EnsureAudioSources()
    {
        if (sourceMusicMenu == null)
        {
            sourceMusicMenu = gameObject.AddComponent<AudioSource>();
        }
        if (sourceMusicGame == null)
        {
            sourceMusicGame = gameObject.AddComponent<AudioSource>();
        }

        ConfigureSource(sourceMusicMenu, musicMenuClip);
        ConfigureSource(sourceMusicGame, musicGameClip);
    }

    private void ConfigureSource(AudioSource source, AudioClip clip)
    {
        source.clip = clip;
        source.loop = true;
        source.playOnAwake = false;
        source.volume = 0f;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayForScene(scene.name, instant: false);

        // btnVolumeOn/btnVolumeMute di-drag manual per-scene di Inspector. Kalau scene baru
        // ini tidak punya tombol tersebut ter-assign (field dibiarkan kosong di Inspector),
        // RefreshVolumeButtonUI() akan skip dengan aman tanpa error.
        RefreshVolumeButtonUI();
    }

    /// <summary>
    /// Menentukan track mana yang harus diputar berdasarkan nama scene,
    /// lalu melakukan transisi (instant saat start, atau fade saat pindah scene).
    /// </summary>
    private void PlayForScene(string sceneName, bool instant)
    {
        bool isMainGame = string.Equals(sceneName, mainGameSceneName, StringComparison.Ordinal);
        MusicTrack target = isMainGame ? MusicTrack.Game : MusicTrack.Menu;

        if (target == currentTrack)
        {
            // Sudah di track yang benar (misal pindah antar scene yang sama-sama pakai Musik Menu),
            // tidak perlu fade ulang.
            return;
        }

        currentTrack = target;

        AudioSource sourceIn = target == MusicTrack.Menu ? sourceMusicMenu : sourceMusicGame;
        AudioSource sourceOut = target == MusicTrack.Menu ? sourceMusicGame : sourceMusicMenu;
        float targetVolume = target == MusicTrack.Menu ? musicMenuVolume : musicGameVolume;

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
            // Coroutine sebelumnya mungkin terhenti di tengah fade out (outSource belum di-Pause).
            // Pause sekarang juga supaya track yang ditinggalkan tidak terus main di background
            // dan posisinya (time) tidak ke-reset seperti yang terjadi kalau dipanggil Stop().
            sourceOut.Pause();
        }

        if (instant)
        {
            sourceOut.Pause();
            sourceOut.volume = 0f;
            sourceIn.volume = targetVolume;
            if (sourceIn.clip != null)
            {
                sourceIn.Play();
            }
        }
        else
        {
            fadeCoroutine = StartCoroutine(CrossfadeRoutine(sourceOut, sourceIn, targetVolume));
        }
    }

    /// <summary>
    /// Fade out musik lama selama fadeDuration/2, lalu fade in musik baru selama fadeDuration/2.
    /// Total durasi = fadeDuration (sesuai request: fade out lalu fade in, 1 detik).
    /// </summary>
    private IEnumerator CrossfadeRoutine(AudioSource outSource, AudioSource inSource, float targetVolume)
    {
        float half = fadeDuration / 2f;

        // --- Fade out musik lama ---
        float startVolume = outSource.volume;
        float t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            outSource.volume = Mathf.Lerp(startVolume, 0f, half <= 0f ? 1f : t / half);
            yield return null;
        }
        outSource.volume = 0f;
        // Pause (bukan Stop) supaya AudioSource.time tetap tersimpan di posisi sekarang —
        // dengan begitu kalau track ini diputar lagi nanti (kembali ke scene yang sama jenis
        // musiknya), playback lanjut dari sini, bukan restart dari 0.
        outSource.Pause();

        // --- Fade in musik baru ---
        if (inSource.clip != null && !inSource.isPlaying)
        {
            inSource.volume = 0f;
            // Play() pada AudioSource yang sebelumnya di-Pause (bukan di-Stop) otomatis
            // melanjutkan dari AudioSource.time terakhir.
            inSource.Play();
        }

        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            inSource.volume = Mathf.Lerp(0f, targetVolume, half <= 0f ? 1f : t / half);
            yield return null;
        }
        inSource.volume = targetVolume;

        fadeCoroutine = null;
    }

    // ------------------------------------------------------------
    //  MUTE — dipanggil dari UI (mis. tombol speaker di BtnPause / Settings)
    // ------------------------------------------------------------

    /// <summary>Toggle mute on/off. Cocok dipasang langsung di OnClick() tombol UI.</summary>
    public void ToggleMute()
    {
        SetMute(!IsMuted);
    }

    /// <summary>Set status mute secara eksplisit. Global — mematikan seluruh audio (musik & SFX).</summary>
    public void SetMute(bool mute)
    {
        IsMuted = mute;
        AudioListener.volume = mute ? 0f : 1f;
        RefreshVolumeButtonUI();
        OnMuteChanged?.Invoke(IsMuted);
    }

    /// <summary>
    /// Tampilkan salah satu tombol (BtnVolumeOn / BtnVolumeMute) sesuai IsMuted,
    /// dan sembunyikan yang lain. Field yang belum di-assign di Inspector (null)
    /// untuk scene ini dilewati saja, tidak menyebabkan error.
    /// </summary>
    private void RefreshVolumeButtonUI()
    {
        if (btnVolumeOn != null)
        {
            btnVolumeOn.SetActive(!IsMuted);
        }
        if (btnVolumeMute != null)
        {
            btnVolumeMute.SetActive(IsMuted);
        }
    }
}