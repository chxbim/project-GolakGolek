using UnityEngine;

/// <summary>
/// Pasang di GameObject parent rak (sama dengan ShelfUnit).
/// Drag prefab item dari Assets/Art/Props/Prefabs/... ke field ItemPrefab di Inspector.
///
/// Koordinat posisiX/Y/Z dari API = world position item pertama (C1, Level 1).
/// Item berikutnya dalam 1 baris  : X += jarakHorizontal
/// Level berikutnya               : Y += jarakVertikal
/// Z tetap sama untuk semua item.
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

        // posisiX/Y/Z dari API = world position item pertama (L1, R1, C1)
        Vector3 basePos = new Vector3(itemData.posisiX, itemData.posisiY, itemData.posisiZ);
        int baris = Mathf.Max(1, itemData.jumlahBaris);
        int spawnCount = 0;

        // Debug — konfirmasi nilai yang masuk dari API
        Debug.Log($"[ItemSpawner] {gameObject.name} basePos={basePos} " +
                  $"jarakH={itemData.jarakHorizontal} jarakV={itemData.jarakVertikal}");

        for (int level = 0; level < levelCount; level++)
        {
            float worldY = basePos.y + (level * itemData.jarakVertikal);

            for (int row = 0; row < baris; row++)
            {
                for (int col = 0; col < itemsPerRow; col++)
                {
                    Vector3 worldPos = new Vector3(
                        basePos.x + (col * itemData.jarakHorizontal),
                        worldY,
                        basePos.z
                    );

                    // Instantiate dulu tanpa posisi, lalu set eksplisit
                    // supaya posisi baked di prefab tidak override worldPos
                    GameObject spawned = Instantiate(itemPrefab);
                    spawned.transform.position = worldPos;
                    Debug.Log($"[ItemSpawner] {spawned.name} → set pos {worldPos} → actual {spawned.transform.position}");
                    spawned.transform.rotation = Quaternion.identity;
                    spawned.name = $"{itemPrefab.name}_L{level + 1}_R{row + 1}_C{col + 1}";
                    spawnCount++;
                }
            }
        }

        hasSpawned = true;
        Debug.Log($"[ItemSpawner] {gameObject.name} → spawn {spawnCount}x '{itemPrefab.name}' " +
                  $"({levelCount} level × {baris} baris × {itemsPerRow} item/baris) " +
                  $"base world pos: {basePos}");
    }

    // ── Clear ─────────────────────────────────────────────────

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