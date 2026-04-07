using UnityEngine;

/// <summary>
/// Pasang di child isTrigger cube.
/// Tugasnya satu: kasih tahu ShelfUnit di parent saat player masuk/keluar.
/// ShelfUnit TIDAK perlu ada Collider — dia hanya logic container.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ProximityDetector : MonoBehaviour
{
    private ShelfUnit parentShelf;

    private void Awake()
    {
        parentShelf = GetComponentInParent<ShelfUnit>();

        if (parentShelf == null)
            Debug.LogError($"[ProximityDetector] Tidak menemukan ShelfUnit di parent dari {gameObject.name}. " +
                           "Pastikan ShelfUnit ada di GameObject parent.");

        // Pastikan collider ini memang isTrigger
        Collider col = GetComponent<Collider>();
        if (!col.isTrigger)
            Debug.LogWarning($"[ProximityDetector] Collider di {gameObject.name} bukan isTrigger. Harap centang isTrigger di Inspector.");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        parentShelf?.SetPlayerInRange(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        parentShelf?.SetPlayerInRange(false);
    }
}