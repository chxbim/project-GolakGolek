using UnityEngine;

/// <summary>
/// Pasang di GameObject parent rak (sama dengan ShelfUnit).
/// Drag prefab item dari Assets/Art/Props/Prefabs/... ke field ItemPrefab di Inspector.
///
/// Koordinat posisiX/Y/Z dari API = local position relative to parent rak (C1, Level 1).
/// Item berikutnya dalam 1 baris  : X += jarakHorizontal
/// Level berikutnya               : Y += jarakVertikal
/// Z tetap sama untuk semua item.
/// Scale dan rotation per-item dari API (scale_x/y/z, rotate_x/y/z).
/// Kulkas_02 dan Rak_02 otomatis override levelCount = 1.
/// </summary>
public class ItemSpawnerHorizontalX : MonoBehaviour, IItemSpawner
{
    [Header("Prefab Item")]
    [Tooltip("Drag prefab item dari Assets/Art/Props/Prefabs/...")]
    [SerializeField] private GameObject itemPrefab;

    [Header("Spawn Config")]
    [SerializeField] private int levelCount = 3;
    [SerializeField] private int itemsPerRow = 3;

    private GameItemData itemData;
    private bool hasSpawned = false;

    // ── Awake ─────────────────────────────────────────────────

    private void Awake()
    {
        ShelfUnit shelf = GetComponent<ShelfUnit>();
        if (shelf == null)
        {
            Debug.LogWarning($"[ItemSpawnerHorizontalX] {gameObject.name} — ShelfUnit tidak ditemukan di GameObject yang sama.");
            return;
        }

        // Kulkas_02 dan Rak_02 hanya punya 1 level
        if (shelf.shelfType == ShelfType.Kulkas_02 || shelf.shelfType == ShelfType.Rak_02)
            levelCount = 1;
    }

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
            Debug.LogWarning($"[ItemSpawnerHorizontalX] {gameObject.name} — itemData null.");
            return;
        }

        if (itemPrefab == null)
        {
            Debug.LogWarning($"[ItemSpawnerHorizontalX] {gameObject.name} — itemPrefab belum di-assign di Inspector!");
            return;
        }

        if (hasSpawned) return;

        // posisiX/Y/Z dari API = local position relative to parent rak (L1, R1, C1)
        Vector3 basePos = new Vector3(itemData.posisiX, itemData.posisiY, itemData.posisiZ);
        int baris = Mathf.Max(1, itemData.jumlahBaris);
        int spawnCount = 0;

        // Scale & rotation dari API; fallback scale ke (1,1,1) kalau DB masih 0
        Vector3 scale = new Vector3(itemData.scaleX, itemData.scaleY, itemData.scaleZ);
        if (scale == Vector3.zero) scale = Vector3.one;
        Quaternion rotation = Quaternion.Euler(itemData.rotateX, itemData.rotateY, itemData.rotateZ);

        // Debug — konfirmasi nilai yang masuk dari API
        Debug.Log($"[ItemSpawnerHorizontalX] {gameObject.name} baseLocalPos={basePos} " +
                  $"jarakH(X)={itemData.jarakHorizontal} jarakV(Y)={itemData.jarakVertikal} " +
                  $"scale={scale} rotate=({itemData.rotateX},{itemData.rotateY},{itemData.rotateZ})");

        for (int level = 0; level < levelCount; level++)
        {
            float localY = basePos.y + (level * itemData.jarakVertikal);

            for (int row = 0; row < baris; row++)
            {
                for (int col = 0; col < itemsPerRow; col++)
                {
                    // jarakHorizontal → X offset
                    Vector3 localPos = new Vector3(
                        basePos.x + (col * itemData.jarakHorizontal),
                        localY,
                        basePos.z
                    );

                    // Instantiate sebagai child parent rak, set localPosition eksplisit
                    // supaya prefab baked transform tidak interfere
                    GameObject spawned = Instantiate(itemPrefab, transform);
                    spawned.transform.localPosition = localPos;
                    spawned.transform.localRotation = rotation;
                    spawned.transform.localScale = scale;
                    Debug.Log($"[ItemSpawnerHorizontalX] {spawned.name} → localPos {localPos} → actual {spawned.transform.localPosition}");
                    spawned.name = $"{itemPrefab.name}_L{level + 1}_R{row + 1}_C{col + 1}";
                    spawnCount++;
                }
            }
        }

        hasSpawned = true;
        Debug.Log($"[ItemSpawnerHorizontalX] {gameObject.name} → spawn {spawnCount}x '{itemPrefab.name}' " +
                  $"({levelCount} level × {baris} baris × {itemsPerRow} item/baris) " +
                  $"base local pos: {basePos}");
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