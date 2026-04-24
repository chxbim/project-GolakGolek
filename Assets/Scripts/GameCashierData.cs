using System;

/// <summary>
/// Data sesi game untuk keperluan submit Kasir.
/// Diisi oleh GameManager saat game berlangsung,
/// lalu di-pass ke CartSystem.BuildPayload() saat submit.
/// </summary>
[Serializable]
public class GameCashierData
{
    public string playerId;
    public string mode;           // "TimeAttack" | "Golek"
    public float waktuSelesai;    // detik elapsed; 0 kalau mode Golek
    public int itemDitemukan;     // diisi dari CartSystem.GetEntries().Count saat submit
    public string timestamp;      // diisi saat BuildPayload dipanggil
}