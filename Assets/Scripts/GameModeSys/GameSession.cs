// GameSession.cs
// Static class — tidak perlu attach ke GameObject.
// Carrier untuk data sesi game yang hanya hidup in-memory (tidak di-persist ke disk).
// Di-set di SelectMode scene, dibaca oleh GameModeController di MainGameScene.

public static class GameSession
{
    // Mode yang dipilih player di SelectMode
    // Default TimeAttack supaya tidak null kalau somehow terlewat di-set
    public static GameMode SelectedMode { get; set; } = GameMode.time_attack;

    // Reset saat player kembali ke SelectMode (opsional, bisa dipanggil SceneController)
    public static void Reset()
    {
        SelectedMode = GameMode.time_attack;
    }
}