/// <summary>
/// Interface untuk semua objek yang bisa di-interact oleh player.
/// Pasang di komponen manapun yang mau bisa di-tap.
/// </summary>
public interface IInteractable
{
    /// <summary>Dipanggil saat player berhasil tap/raycast ke objek ini.</summary>
    void Interact();

    /// <summary>Nama display objek untuk UI / debug.</summary>
    string DisplayName { get; }
}