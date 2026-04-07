using UnityEngine;

/// <summary>
/// Script test sementara — JANGAN dibawa ke build final.
/// Pasang di GameObject kosong mana saja di scene.
/// Jalankan game, lalu klik tombol di Inspector untuk test.
///
/// Cara pakai:
///   1. Attach script ini ke GameObject kosong
///   2. Play scene
///   3. Di Inspector, klik "Test GET Items" atau "Test POST Cart"
///   4. Buka Console — lihat log hasilnya
/// </summary>
public class APITester : MonoBehaviour
{
    [Header("Klik tombol di bawah saat Play Mode")]
    [SerializeField] private bool testGET = false;
    [SerializeField] private bool testPOST = false;

    private void Update()
    {
        // Inspector toggle sebagai "tombol" — reset setelah dipanggil
        if (testGET)
        {
            testGET = false;
            RunTestGET();
        }

        if (testPOST)
        {
            testPOST = false;
            RunTestPOST();
        }
    }

    // ── Test GET ─────────────────────────────────────────────

    private void RunTestGET()
    {
        Debug.Log("[APITester] Memulai test GET...");

        if (APIManager.Instance == null)
        {
            Debug.LogError("[APITester] APIManager tidak ada di scene! Pastikan APIManager sudah dipasang.");
            return;
        }

        APIManager.Instance.FetchItems(
            onSuccess: items =>
            {
                Debug.Log($"[APITester] GET BERHASIL — {items.Count} item diterima:");
                foreach (var item in items)
                    Debug.Log($"   • {item}");
            },
            onError: err =>
            {
                Debug.LogError($"[APITester] GET GAGAL — {err}");
                Debug.LogWarning("[APITester] Kemungkinan penyebab:\n" +
                                 "  1. Tidak ada koneksi internet\n" +
                                 "  2. URL endpoint salah\n" +
                                 "  3. Server JTV sedang down");
            }
        );
    }

    // ── Test POST ─────────────────────────────────────────────

    private void RunTestPOST()
    {
        Debug.Log("[APITester] Memulai test POST...");

        if (APIManager.Instance == null)
        {
            Debug.LogError("[APITester] APIManager tidak ada di scene!");
            return;
        }

        // Buat payload dummy untuk test
        var payload = new CartPayload
        {
            playerId = "TEST_PLAYER_001",
            totalHarga = 18000f,
            timestamp = System.DateTime.UtcNow.ToString("o"),
            items = new System.Collections.Generic.List<CartItemPayload>
            {
                new CartItemPayload { namaItem = "Saus Berisik",  kategori = "makanan", harga = 10000f, quantity = 1 },
                new CartItemPayload { namaItem = "Teh Bunga",     kategori = "makanan", harga = 4500f,  quantity = 1 },
                new CartItemPayload { namaItem = "Kapal Asin",    kategori = "makanan", harga = 5000f,  quantity = 1 }
            }
        };

        APIManager.Instance.PostCart(
            payload,
            onSuccess: response =>
            {
                Debug.Log($"[APITester] POST BERHASIL — Response dari server:\n{response}");
                Debug.Log("[APITester] Artinya: server JTV menerima data cart dan bisa di-read/write.");
            },
            onError: err =>
            {
                Debug.LogError($"[APITester] POST GAGAL — {err}");
                Debug.LogWarning("[APITester] Kemungkinan penyebab:\n" +
                                 "  1. Endpoint cart belum ada di backend JTV\n" +
                                 "  2. Server tidak accept POST method\n" +
                                 "  3. Perlu API key / auth header\n" +
                                 "  → Tanyakan ke tim backend JTV apakah ada endpoint POST untuk cart.");
            }
        );
    }
}