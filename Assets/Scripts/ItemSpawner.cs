using UnityEngine;

/// <summary>
/// Pasang di GameObject parent rak (sama dengan ShelfUnit).
/// Drag prefab item dari Assets/Art/Props/Prefabs/... ke field ItemPrefab di Inspector.
/// Posisi spawn dihitung otomatis dari data API saat Play.
/// </summary>
public class ItemSpawner : MonoBehaviour
{
    [Header("Prefab Item")]
    [Tooltip("Drag prefab item dari Assets/Art/Props/Prefabs/...")]
    [SerializeField] private GameObject itemPrefab;

    [Header("Spawn Config")]
    [SerializeField] private int levelCount = 3;
    [SerializeField] private int itemsPerRow = 3;

    private GameItemData itemData;
    private bool hasSpawned = false;

    // ── Dipanggil ShelfManager ────────────────────────────────

    public void SetItemDataAndSpawn(GameItemData data)
    {
        itemData = data;
        SpawnItems();
    }

    // ── Spawn ─────────────────────────────────────────────────

    private void SpawnItems()
    {
        if (itemData == null)
        {
            Debug.LogWarning($"[ItemSpawner] {gameObject.name} — itemData null.");
            return;
        }

        if (itemPrefab == null)
        {
            Debug.LogWarning($"[ItemSpawner] {gameObject.name} — itemPrefab belum di-assign di Inspector!");
            return;
        }

        if (hasSpawned) return;

        Vector3 basePos = new Vector3(itemData.posisiX, itemData.posisiY, itemData.posisiZ);
        int baris = Mathf.Max(1, itemData.jumlahBaris);
        int spawnCount = 0;

        for (int level = 0; level < levelCount; level++)
        {
            float levelY = basePos.y + (level * itemData.jarakVertikal);

            for (int row = 0; row < baris; row++)
            {
                for (int col = 0; col < itemsPerRow; col++)
                {
                    Vector3 localPos = new Vector3(
                        basePos.x + (col * itemData.jarakHorizontal),
                        levelY,
                        basePos.z
                    );
                    Vector3 worldPos = transform.TransformPoint(localPos);

                    GameObject spawned = Instantiate(itemPrefab, worldPos, Quaternion.identity, transform);
                    spawned.name = $"{itemPrefab.name}_L{level + 1}_R{row + 1}_C{col + 1}";
                    spawnCount++;
                }
            }
        }

        hasSpawned = true;
        Debug.Log($"[ItemSpawner] {gameObject.name} → spawn {spawnCount}x '{itemPrefab.name}' " +
                  $"({levelCount} level × {baris} baris × {itemsPerRow} item/baris)");
    }

    // ── Clear (opsional, untuk reload) ───────────────────────

    public void ClearSpawnedItems()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.GetComponent<ProximityDetector>() == null)
                Destroy(child.gameObject);
        }
        hasSpawned = false;
    }
}