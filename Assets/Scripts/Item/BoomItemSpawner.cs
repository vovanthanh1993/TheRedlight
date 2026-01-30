using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawner để quản lý spawn boom items
/// Khi player nhặt boom item, sẽ spawn boom item mới ở spawn point ngẫu nhiên
/// </summary>
public class BoomItemSpawner : MonoBehaviour
{
    public static BoomItemSpawner Instance { get; private set; }
    
    [Header("Spawn Settings")]
    [Tooltip("Prefab của boom item")]
    [SerializeField] private GameObject boomItemPrefab;
    
    [Header("Spawn Points")]
    [Tooltip("Danh sách các spawn points cho boom item")]
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
    
    [Tooltip("GameObject cha chứa tất cả các spawn points (sẽ tự động lấy tất cả các con)")]
    [SerializeField] private Transform spawnPointsParent;
    
    [Header("Settings")]
    [Tooltip("Số lượng boom item tối đa trên map cùng lúc")]
    [SerializeField] private int maxBoomItemsOnMap = 1;
    
    private List<GameObject> spawnedBoomItems = new List<GameObject>();
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    
    private void Start()
    {
        InitializeSpawnPoints();
        
        // Spawn boom item ban đầu nếu có
        if (maxBoomItemsOnMap > 0 && boomItemPrefab != null)
        {
            SpawnBoomItem();
        }
    }
    
    /// <summary>
    /// Khởi tạo danh sách spawn points
    /// </summary>
    private void InitializeSpawnPoints()
    {
        spawnPoints.Clear();
        
        // Nếu có spawnPointsParent, lấy tất cả các con
        if (spawnPointsParent != null)
        {
            foreach (Transform child in spawnPointsParent)
            {
                if (child != null)
                {
                    spawnPoints.Add(child);
                }
            }
        }
        
        // Thêm các spawn points đã gán trực tiếp trong Inspector
        // (nếu có spawn points trong list nhưng không có parent)
        
        if (spawnPoints.Count == 0)
        {
            Debug.LogWarning("BoomItemSpawner: Không có spawn point nào! Vui lòng gán spawn points.");
        }
        else
        {
            Debug.Log($"BoomItemSpawner: Đã khởi tạo {spawnPoints.Count} spawn points.");
        }
    }
    
    /// <summary>
    /// Spawn boom item tại spawn point ngẫu nhiên
    /// </summary>
    /// <param name="avoidPosition">Vị trí cần tránh khi spawn (ví dụ: vị trí vừa nhặt)</param>
    public void SpawnBoomItem(Vector3 avoidPosition = default)
    {
        if (boomItemPrefab == null)
        {
            Debug.LogWarning("BoomItemSpawner: Boom item prefab chưa được gán!");
            return;
        }
        
        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            Debug.LogWarning("BoomItemSpawner: Không có spawn point nào!");
            return;
        }
        
        // Kiểm tra số lượng boom item trên map
        CleanDestroyedItems();
        if (spawnedBoomItems.Count >= maxBoomItemsOnMap)
        {
            Debug.Log($"BoomItemSpawner: Đã đạt số lượng tối đa ({maxBoomItemsOnMap}) boom items trên map.");
            return;
        }
        
        // Tạo danh sách spawn points có thể dùng (tránh vị trí vừa nhặt nếu có)
        List<int> availableIndices = new List<int>();
        float minDistance = 3f; // Khoảng cách tối thiểu từ vị trí vừa nhặt
        
        for (int i = 0; i < spawnPoints.Count; i++)
        {
            if (spawnPoints[i] == null)
                continue;
            
            // Nếu có vị trí cần tránh, kiểm tra khoảng cách
            if (avoidPosition != default)
            {
                float distance = Vector3.Distance(spawnPoints[i].position, avoidPosition);
                if (distance < minDistance)
                {
                    continue; // Bỏ qua spawn point quá gần
                }
            }
            
            availableIndices.Add(i);
        }
        
        // Nếu không còn spawn point nào phù hợp, dùng tất cả
        if (availableIndices.Count == 0)
        {
            for (int i = 0; i < spawnPoints.Count; i++)
            {
                if (spawnPoints[i] != null)
                {
                    availableIndices.Add(i);
                }
            }
        }
        
        if (availableIndices.Count == 0)
        {
            Debug.LogWarning("BoomItemSpawner: Không có spawn point hợp lệ!");
            return;
        }
        
        // Chọn ngẫu nhiên một spawn point từ danh sách có thể dùng
        int randomIndex = availableIndices[Random.Range(0, availableIndices.Count)];
        Transform spawnPoint = spawnPoints[randomIndex];
        
        Vector3 spawnPosition = spawnPoint.position;
        Quaternion spawnRotation = Quaternion.Euler(0, 180, 0); // Rotation y = 180 như các item khác
        
        // Spawn boom item
        GameObject boomItem = Instantiate(boomItemPrefab, spawnPosition, spawnRotation);
        spawnedBoomItems.Add(boomItem);
        
        Debug.Log($"BoomItemSpawner: Đã spawn boom item tại spawn point {randomIndex}.");
    }
    
    /// <summary>
    /// Xóa các boom items đã bị destroy khỏi list
    /// </summary>
    private void CleanDestroyedItems()
    {
        spawnedBoomItems.RemoveAll(item => item == null);
    }
    
    /// <summary>
    /// Được gọi khi player nhặt boom item
    /// </summary>
    /// <param name="collectedPosition">Vị trí mà player vừa nhặt boom item (để tránh spawn lại ở đó)</param>
    /// <param name="collectedItem">Boom item vừa được nhặt (để remove khỏi list)</param>
    public void OnBoomItemCollected(Vector3 collectedPosition = default, GameObject collectedItem = null)
    {
        Debug.Log("BoomItemSpawner: OnBoomItemCollected được gọi.");
        
        // Remove item vừa nhặt khỏi list ngay lập tức
        if (collectedItem != null)
        {
            bool removed = spawnedBoomItems.Remove(collectedItem);
            Debug.Log($"BoomItemSpawner: Remove item khỏi list: {removed}. Số lượng còn lại: {spawnedBoomItems.Count}");
        }
        
        CleanDestroyedItems();
        Debug.Log($"BoomItemSpawner: Sau khi clean, số lượng: {spawnedBoomItems.Count}, max: {maxBoomItemsOnMap}");
        
        // Spawn boom item mới ở spawn point khác (tránh vị trí vừa nhặt)
        SpawnBoomItem(collectedPosition);
    }
    
    /// <summary>
    /// Xóa tất cả boom items đã spawn
    /// </summary>
    public void ClearAllBoomItems()
    {
        foreach (var item in spawnedBoomItems)
        {
            if (item != null)
            {
                Destroy(item);
            }
        }
        spawnedBoomItems.Clear();
    }
    
    /// <summary>
    /// Reset spawner (dùng khi restart level)
    /// </summary>
    public void ResetSpawner()
    {
        ClearAllBoomItems();
        
        // Spawn boom item ban đầu
        if (maxBoomItemsOnMap > 0 && boomItemPrefab != null)
        {
            SpawnBoomItem();
        }
    }
}
