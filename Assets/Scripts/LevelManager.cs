using UnityEngine;

/// <summary>
/// Quản lý việc load level prefab từ Resources
/// và progress riêng cho từng level (ví dụ: số lượng EnergyItem cần nhặt)
/// </summary>
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }
    
    [Header("Level Settings")]
    [Tooltip("Parent object để chứa level prefab (nếu null sẽ tạo mới)")]
    [SerializeField] private Transform levelParent;
    
    [Tooltip("Đường dẫn đến folder level trong Resources")]
    [SerializeField] private string levelResourcePath = "Level";

    // Level prefab hiện tại đang được load
    private GameObject currentLevelInstance;

    // Đếm số điểm EnergyItem đã nhặt (đã bay vào Port) trong level hiện tại
    private int collectedEnergyPoints = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        
        // Tạo parent nếu chưa có
        if (levelParent == null)
        {
            GameObject parentObj = new GameObject("LevelParent");
            levelParent = parentObj.transform;
        }
    }
    
    /// <summary>
    /// Load level prefab từ Resources dựa trên level number
    /// Level 16 sẽ load Level1, Level 17 sẽ load Level2, cứ 15 level là lặp lại
    /// </summary>
    /// <param name="levelNumber">Số level (1, 2, 3, ...)</param>
    /// <returns>GameObject của level đã được instantiate, null nếu không tìm thấy</returns>
    public GameObject LoadLevel(int levelNumber)
    {
        // Xóa level cũ nếu có
        UnloadCurrentLevel();

        // Reset progress energy cho level mới
        ResetEnergyProgress();
        
        // Map level number về prefab number theo chu kỳ 15 level
        // Level 1-15: Level1-Level15
        // Level 16-30: Level1-Level15 (lặp lại)
        // Level 31-45: Level1-Level15 (lặp lại)
        // ...
        int prefabNumber = ((levelNumber - 1) % 15) + 1;
        
        // Tên prefab: Level1, Level2, Level3, ...
        string prefabName = $"Level{prefabNumber}";
        string resourcePath = $"{levelResourcePath}/{prefabName}";
        
        // Load prefab từ Resources
        GameObject levelPrefab = Resources.Load<GameObject>(resourcePath);
        
        if (levelPrefab == null)
        {
            Debug.LogError($"LevelManager: Không tìm thấy level prefab tại '{resourcePath}'!");
            return null;
        }
        
        // Instantiate level prefab
        currentLevelInstance = Instantiate(levelPrefab, levelParent);
        currentLevelInstance.name = prefabName; // Đặt tên rõ ràng
        
        Debug.Log($"LevelManager: Đã load level {levelNumber} (sử dụng prefab {prefabName}) từ '{resourcePath}'");
        
        return currentLevelInstance;
    }
    
    /// <summary>
    /// Xóa level hiện tại khỏi scene
    /// </summary>
    public void UnloadCurrentLevel()
    {
        if (currentLevelInstance != null)
        {
            Debug.Log($"LevelManager: Xóa level '{currentLevelInstance.name}'");
            Destroy(currentLevelInstance);
            currentLevelInstance = null;
        }
    }
    
    /// <summary>
    /// Lấy level instance hiện tại
    /// </summary>
    public GameObject GetCurrentLevel()
    {
        return currentLevelInstance;
    }
    
    /// <summary>
    /// Kiểm tra xem có level nào đang được load không
    /// </summary>
    public bool HasLevelLoaded()
    {
        return currentLevelInstance != null;
    }

    #region Energy Progress

    /// <summary>
    /// Gọi khi một EnergyItem đã bay vào Port (được nhặt thành công)
    /// </summary>
    /// <param name="points">Điểm số của EnergyItem (1 hoặc 5 điểm)</param>
    public void OnEnergyItemArrived(int points)
    {
        collectedEnergyPoints += points;
        Debug.Log($"LevelManager: EnergyItem arrived với {points} điểm. Tổng điểm = {collectedEnergyPoints}/{GetRequiredEnergyPointsForCurrentLevel()}");
        
        // Cập nhật PowerBar nếu có
        if (PowerBar.Instance != null)
        {
            PowerBar.Instance.OnEnergyItemCollected();
        }
    }

    /// <summary>
    /// Lấy số điểm EnergyItem đã nhặt trong level hiện tại
    /// </summary>
    public int GetCollectedEnergyPoints()
    {
        return collectedEnergyPoints;
    }

    /// <summary>
    /// Lấy số điểm EnergyItem cần nhặt cho level hiện tại (tương thích với code cũ)
    /// </summary>
    [System.Obsolete("Sử dụng GetCollectedEnergyPoints() thay thế")]
    public int GetCollectedEnergyItems()
    {
        return collectedEnergyPoints;
    }

    /// <summary>
    /// Lấy số điểm EnergyItem cần nhặt cho level hiện tại
    /// </summary>
    public int GetRequiredEnergyPointsForCurrentLevel()
    {
        int currentLevel = 1;
        if (PlayerPrefs.HasKey("CurrentLevel"))
        {
            currentLevel = PlayerPrefs.GetInt("CurrentLevel");
        }

        // Lấy dữ liệu quest từ JSON để đọc cấu hình requiredEnergyPoints
        QuestData quest = QuestDataStorage.LoadQuest(currentLevel);
        if (quest != null)
        {
            return Mathf.Max(0, quest.requiredEnergyPoints);
        }

        // Nếu không có quest hoặc không cấu hình, mặc định = 0 (không yêu cầu EnergyItem)
        return 0;
    }

    /// <summary>
    /// Lấy số điểm EnergyItem cần nhặt cho level hiện tại (tương thích với code cũ)
    /// </summary>
    [System.Obsolete("Sử dụng GetRequiredEnergyPointsForCurrentLevel() thay thế")]
    public int GetRequiredEnergyItemsForCurrentLevel()
    {
        return GetRequiredEnergyPointsForCurrentLevel();
    }

    /// <summary>
    /// Kiểm tra đã đủ điểm EnergyItem để qua màn chưa
    /// </summary>
    public bool HasCollectedEnoughEnergy()
    {
        int required = GetRequiredEnergyPointsForCurrentLevel();

        // Nếu required = 0 thì không yêu cầu collect energy
        if (required <= 0)
            return true;

        return collectedEnergyPoints >= required;
    }

    /// <summary>
    /// Reset progress EnergyItem (dùng khi load level mới)
    /// </summary>
    public void ResetEnergyProgress()
    {
        collectedEnergyPoints = 0;
        
        // Reset PowerBar nếu có
        if (PowerBar.Instance != null)
        {
            PowerBar.Instance.ResetPowerBar();
        }
        
        // Reset SpeedSkill nếu có
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.ResetSpeedSkill();
        }
    }

    #endregion
}
