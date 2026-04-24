using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Inventory UI — menampilkan isi CartSystem ke dalam slot-slot di layar.
/// Inventory TIDAK menyimpan data sendiri; CartSystem adalah sumber kebenaran.
/// Subscribe ke CartSystem.OnCartChanged, lalu render ulang slot.
/// </summary>
public class Inventory : MonoBehaviour
{
    // ── Singleton ────────────────────────────────────────────
    public static Inventory Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────
    [Header("UI References")]
    [SerializeField] private GameObject inventorySlotParent;

    [Header("Debug")]
    [SerializeField] private bool logRefresh = false;

    // ── Runtime ──────────────────────────────────────────────
    private readonly List<Slots> allSlots = new List<Slots>();

    // ── Lifecycle ─────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // FIX: GetComponentsInChildren (plural) — versi singular hanya ambil 1 slot pertama!
        allSlots.AddRange(inventorySlotParent.GetComponentsInChildren<Slots>());
        Debug.Log($"[Inventory] {allSlots.Count} slot ditemukan.");
    }

    private void Start()
    {
        if (CartSystem.Instance == null)
        {
            Debug.LogWarning("[Inventory] CartSystem tidak ditemukan di scene. " +
                             "Pastikan CartSystem ada dan Awake-nya jalan lebih dulu.");
            return;
        }

        CartSystem.Instance.OnCartChanged += RefreshFromCart;
        RefreshFromCart(); // render state awal (biasanya kosong)
    }

    private void OnDestroy()
    {
        if (CartSystem.Instance != null)
            CartSystem.Instance.OnCartChanged -= RefreshFromCart;
    }

    // ── Core ──────────────────────────────────────────────────

    /// <summary>
    /// Baca semua entry dari CartSystem dan tampilkan ke slot UI.
    /// Dipanggil otomatis setiap kali CartSystem berubah.
    /// </summary>
    private void RefreshFromCart()
    {
        // Reset semua slot
        foreach (Slots slot in allSlots)
            slot.ClearSlot();

        var entries = CartSystem.Instance.GetEntries();

        for (int i = 0; i < entries.Count; i++)
        {
            if (i >= allSlots.Count)
            {
                Debug.LogWarning($"[Inventory] Slot tidak cukup! " +
                                 $"Cart punya {entries.Count} entry, tapi hanya ada {allSlots.Count} slot.");
                break;
            }
            allSlots[i].SetItem(entries[i].Item, entries[i].Quantity);
        }

        if (logRefresh)
            Debug.Log($"[Inventory] Refresh — {entries.Count} entry ditampilkan di {allSlots.Count} slot.");
    }

    // ── Public helper (opsional, untuk keperluan lain) ────────

    /// <summary>Jumlah slot yang tersedia di UI.</summary>
    public int SlotCount => allSlots.Count;

    /// <summary>Berapa slot yang sedang terisi.</summary>
    public int UsedSlotCount => CartSystem.Instance != null ? CartSystem.Instance.GetEntries().Count : 0;
}