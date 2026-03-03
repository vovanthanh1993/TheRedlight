using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawner để quản lý spawn energy items
/// Khi player nhặt energy item, sẽ spawn energy item mới ở spawn point ngẫu nhiên
/// </summary>
public class EnergyItemSpawner : MonoBehaviour
{
    public static EnergyItemSpawner Instance { get; private set; }
    
    [Header("Spawn Settings")]
    [Tooltip("Prefab của energy item")]
    [SerializeField] private GameObject energyItemPrefab;
    
    [Header("Spawn Points")]
    [Tooltip("Danh sách các spawn points cho energy item")]
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
    
    [Tooltip("GameObject cha chứa tất cả các spawn points (sẽ tự động lấy tất cả các con)")]
    [SerializeField] private Transform spawnPointsParent;
    
    [Header("Settings")]
    [Tooltip("Số lượng energy item tối đa trên map cùng lúc")]
    [SerializeField] private int maxEnergyItemsOnMap = 1;
    
    private List<GameObject> spawnedEnergyItems = new List<GameObject>();
    
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
        
        // Spawn energy item ban đầu nếu có
        if (maxEnergyItemsOnMap > 0 && energyItemPrefab != null)
        {
            SpawnEnergyItem();
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
            Debug.LogWarning("EnergyItemSpawner: Không có spawn point nào! Vui lòng gán spawn points.");
        }
        else
        {
            Debug.Log($"EnergyItemSpawner: Đã khởi tạo {spawnPoints.Count} spawn points.");
        }
    }
    
    /// <summary>
    /// Spawn energy item tại spawn point ngẫu nhiên
    /// </summary>
    /// <param name="avoidPosition">Vị trí cần tránh khi spawn (ví dụ: vị trí vừa nhặt)</param>
    public void SpawnEnergyItem(Vector3 avoidPosition = default)
    {
        if (energyItemPrefab == null)
        {
            Debug.LogWarning("EnergyItemSpawner: Energy item prefab chưa được gán!");
            return;
        }
        
        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            Debug.LogWarning("EnergyItemSpawner: Không có spawn point nào!");
            return;
        }
        
        // Kiểm tra số lượng energy item trên map
        CleanDestroyedItems();
        if (spawnedEnergyItems.Count >= maxEnergyItemsOnMap)
        {
            Debug.Log($"EnergyItemSpawner: Đã đạt số lượng tối đa ({maxEnergyItemsOnMap}) energy items trên map.");
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
            Debug.LogWarning("EnergyItemSpawner: Không có spawn point hợp lệ!");
            return;
        }
        
        // Chọn ngẫu nhiên một spawn point từ danh sách có thể dùng
        int randomIndex = availableIndices[Random.Range(0, availableIndices.Count)];
        Transform spawnPoint = spawnPoints[randomIndex];
        
        Vector3 spawnPosition = spawnPoint.position;
        Quaternion spawnRotation = Quaternion.Euler(0, 180, 0); // Rotation y = 180 như các item khác
        
        // Spawn energy item
        GameObject energyItem = Instantiate(energyItemPrefab, spawnPosition, spawnRotation);
        spawnedEnergyItems.Add(energyItem);
        
        Debug.Log($"EnergyItemSpawner: Đã spawn energy item tại spawn point {randomIndex}.");
    }
    
    /// <summary>
    /// Xóa các energy items đã bị destroy khỏi list
    /// </summary>
    private void CleanDestroyedItems()
    {
        spawnedEnergyItems.RemoveAll(item => item == null);
    }
    
    /// <summary>
    /// Được gọi khi player nhặt energy item
    /// </summary>
    /// <param name="collectedPosition">Vị trí mà player vừa nhặt energy item (để tránh spawn lại ở đó)</param>
    /// <param name="collectedItem">Energy item vừa được nhặt (để remove khỏi list)</param>
    public void OnEnergyItemCollected(Vector3 collectedPosition = default, GameObject collectedItem = null)
    {
        Debug.Log("EnergyItemSpawner: OnEnergyItemCollected được gọi.");
        
        // Remove item vừa nhặt khỏi list ngay lập tức
        if (collectedItem != null)
        {
            bool removed = spawnedEnergyItems.Remove(collectedItem);
            Debug.Log($"EnergyItemSpawner: Remove item khỏi list: {removed}. Số lượng còn lại: {spawnedEnergyItems.Count}");
        }
        
        CleanDestroyedItems();
        Debug.Log($"EnergyItemSpawner: Sau khi clean, số lượng: {spawnedEnergyItems.Count}, max: {maxEnergyItemsOnMap}");
        
        // Spawn energy item mới ở spawn point khác (tránh vị trí vừa nhặt)
        SpawnEnergyItem(collectedPosition);
    }
    
    /// <summary>
    /// Xóa tất cả energy items đã spawn
    /// </summary>
    public void ClearAllEnergyItems()
    {
        foreach (var item in spawnedEnergyItems)
        {
            if (item != null)
            {
                Destroy(item);
            }
        }
        spawnedEnergyItems.Clear();
    }
    
    /// <summary>
    /// Reset spawner (dùng khi restart level)
    /// </summary>
    public void ResetSpawner()
    {
        ClearAllEnergyItems();
        
        // Spawn energy item ban đầu
        if (maxEnergyItemsOnMap > 0 && energyItemPrefab != null)
        {
            SpawnEnergyItem();
        }
    }
}
