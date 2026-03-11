using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    public QuestData currentQuest;
    
    // Progress cho item collection
    public Dictionary<ItemType, int> progress = new Dictionary<ItemType, int>();

    private bool questCompleted = false;
    
    private float gameStartTime;
    private float gameElapsedTime;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        // Load quest từ JSON dựa trên level hiện tại
        LoadQuestFromJSON();
        
        // Khởi tạo progress
        questCompleted = false;
        gameStartTime = Time.time;
        gameElapsedTime = 0f;

        // Khởi tạo progress (có thể không có objectives nếu chỉ cần EnergyItem)
        if (currentQuest != null)
        {
            progress.Clear();
            
            if (currentQuest.objectives != null && currentQuest.objectives.Length > 0)
            {
            foreach (var obj in currentQuest.objectives)
            {
                // Khởi tạo progress cho từng loại item
                progress[obj.itemType] = 0;
            }
            
            // Khởi tạo objectives panel
            if (GUIPanel.Instance != null && GUIPanel.Instance.objectivesPanelComponent != null)
            {
                GUIPanel.Instance.objectivesPanelComponent.InitializeObjectives();
            }
        }
        else
        {
                // Không có objectives (chỉ cần nhặt EnergyItem)
                Debug.Log($"QuestManager: Level {currentQuest.questId} chỉ cần nhặt {currentQuest.requiredEnergyPoints} điểm EnergyItem");
            }
        }
        else
        {
            Debug.LogError("QuestManager: currentQuest là null!");
        }
        
        // Hiển thị hint panel cho level 1
        int currentLevel = GetCurrentLevelFromScene();
        if (currentLevel == 1 && GUIPanel.Instance != null && GUIPanel.Instance.hintPanel != null)
        {
            // Delay một frame để đảm bảo tất cả UI đã được khởi tạo
            StartCoroutine(ShowHintForLevel1());
        }
    }
    
    /// <summary>
    /// Hiển thị hint panel cho level 1 và pause game
    /// </summary>
    System.Collections.IEnumerator ShowHintForLevel1()
    {
        yield return null; // Đợi một frame để đảm bảo UI đã được khởi tạo
        
        // Đảm bảo game được pause khi hiển thị hint
        Time.timeScale = 0f;
        GUIPanel.Instance.ShowHintPanel(true);
        
        Debug.Log("QuestManager: Đã hiển thị hint panel cho level 1 và pause game");
    }
    
    /// <summary>
    /// Load quest từ JSON dựa trên level hiện tại
    /// </summary>
    void LoadQuestFromJSON()
    {
        int currentLevel = GetCurrentLevelFromScene();
        currentQuest = QuestDataStorage.LoadQuest(currentLevel);
        
        if (currentQuest == null)
        {
            Debug.LogError($"QuestManager: Không thể load quest cho level {currentLevel} từ JSON!");
        }
        else
        {
            Debug.Log($"QuestManager: Đã load quest {currentQuest.questId} cho level {currentLevel}");
        }
    }

    void Update()
    {
        if (!questCompleted)
        {
            gameElapsedTime = Time.time - gameStartTime;
            
            // Kiểm tra thời gian giới hạn
            if (currentQuest != null && currentQuest.timeLimit > 0)
            {
                // Nếu hết thời gian và quest chưa hoàn thành (chưa trigger với EndTag) thì thua
                if (gameElapsedTime >= currentQuest.timeLimit)
                {
                    // Kiểm tra lại questCompleted một lần nữa để tránh race condition
                    // (nếu player vừa trigger EndTag trong cùng frame này)
                    if (!questCompleted)
                    {
                        // Hết thời gian và chưa trigger với EndTag -> thua
                        OnTimeLimitReached();
                        return;
                    }
                }
            }
        }
        
        if (GUIPanel.Instance != null)
        {
        GUIPanel.Instance.SetTime(GetGameTimeFormatted());
        }
    }
    
    /// <summary>
    /// Xử lý khi hết thời gian giới hạn - thua ngay lập tức
    /// </summary>
    private void OnTimeLimitReached()
    {
        // Kiểm tra để tránh gọi nhiều lần
        if (questCompleted)
            return;
        
        // Set questCompleted = true ngay để tránh gọi nhiều lần
        questCompleted = true;
        
        Debug.LogWarning($"QuestManager: Hết thời gian giới hạn ({currentQuest.timeLimit}s) - Thua!");
        
        // Hiển thị lose panel
        if (UIManager.Instance != null && UIManager.Instance.gamePlayPanel != null)
        {
            UIManager.Instance.gamePlayPanel.ShowLosePanel(true);
            Time.timeScale = 0f;
        }
    }

    /// <summary>
    /// Được gọi khi player thả item tại checkpoint
    /// </summary>
    public void OnItemCollected(ItemType itemType)
    {
        if (!progress.ContainsKey(itemType))
            return;

        progress[itemType]++;

        Debug.Log($"{itemType} collected: {progress[itemType]} / {GetRequiredAmount(itemType)}");

        // Cập nhật objectives panel
        if (GUIPanel.Instance != null && GUIPanel.Instance.objectivesPanelComponent != null)
        {
            GUIPanel.Instance.objectivesPanelComponent.UpdateProgress();
        }

        // Kiểm tra xem đã hoàn thành quest chưa sau mỗi lần thả animal
        CheckQuestComplete();
    }
    
    /// <summary>
    /// Lấy số lượng cần thiết cho một loại item
    /// </summary>
    int GetRequiredAmount(ItemType itemType)
    {
        foreach (var obj in currentQuest.objectives)
        {
            if (obj.itemType == itemType)
                return obj.requiredAmount;
        }
        return 0;
    }

    void CheckQuestComplete()
    {
        if (questCompleted)
            return;

        // Kiểm tra nếu có objectives (quest cũ)
        if (currentQuest.objectives != null && currentQuest.objectives.Length > 0)
        {
        foreach (var obj in currentQuest.objectives)
        {
            // Check item objectives
            if (!progress.ContainsKey(obj.itemType) || progress[obj.itemType] < obj.requiredAmount)
                    return;
            }
        }
        // Nếu không có objectives, kiểm tra EnergyItem (quest mới)
        else if (currentQuest.requiredEnergyPoints > 0)
        {
            if (LevelManager.Instance == null || !LevelManager.Instance.HasCollectedEnoughEnergy())
                return;
        }

        questCompleted = true;

        // Tính số sao dựa trên thời gian hoàn thành
        int stars = CalculateStars();
        int reward = GetRewardByStars(stars);
        
        // Lưu reward vào PlayerData
        SaveRewardToPlayerData(reward);
        
        // Lưu số sao vào PlayerLevelData
        SaveStarsToLevelData(stars);
        
        Debug.Log($"Quest hoàn thành! Thời gian: {GetGameTimeFormatted()}, Số sao: {stars}, Reward: {reward}");

        Time.timeScale = 0f;

        UIManager.Instance.gamePlayPanel.ShowWinPanel(true, stars, reward);
        // Trigger next level, reward...
    }
    
    /// <summary>
    /// Kiểm tra và hoàn thành quest khi player đến end gate
    /// Chỉ hoàn thành nếu đã collect đủ tất cả items hoặc đủ EnergyItem
    /// </summary>
    public void CheckAndCompleteQuest()
    {
        if (questCompleted)
            return;
        
        bool canComplete = false;
        
        // Kiểm tra nếu có objectives (quest cũ)
        if (currentQuest.objectives != null && currentQuest.objectives.Length > 0)
        {
        // Kiểm tra xem đã collect đủ tất cả items chưa
        bool allObjectivesCompleted = true;
        foreach (var obj in currentQuest.objectives)
        {
            if (!progress.ContainsKey(obj.itemType) || progress[obj.itemType] < obj.requiredAmount)
            {
                allObjectivesCompleted = false;
                break;
            }
        }
            canComplete = allObjectivesCompleted;
        }
        // Nếu không có objectives, kiểm tra EnergyItem (quest mới)
        else if (currentQuest.requiredEnergyPoints > 0)
        {
            if (LevelManager.Instance != null && LevelManager.Instance.HasCollectedEnoughEnergy())
            {
                canComplete = true;
            }
        }
        else
        {
            // Không có yêu cầu gì, cho phép hoàn thành
            canComplete = true;
        }
        
        if (canComplete)
        {
            // Đã collect đủ, hoàn thành quest
            CheckQuestComplete();
        }
        else
        {
            // Chưa đủ, hiển thị thông báo
            Debug.Log("Chưa collect đủ items hoặc EnergyItem! Vui lòng collect đủ trước khi đến endgate.");
            // Có thể hiển thị UI thông báo ở đây nếu cần
        }
    }
    
    /// <summary>
    /// Kiểm tra xem đã collect đủ items chưa
    /// </summary>
    public bool IsQuestObjectivesCompleted()
    {
        if (currentQuest == null || currentQuest.objectives == null)
            return false;
        
        foreach (var obj in currentQuest.objectives)
        {
            if (!progress.ContainsKey(obj.itemType) || progress[obj.itemType] < obj.requiredAmount)
            {
                return false;
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// Lấy số level hiện tại từ PlayerPrefs (được lưu khi load scene từ StartPanel)
    /// </summary>
    int GetCurrentLevelFromScene()
    {
        // Lấy level từ PlayerPrefs (được lưu trong StartPanel khi load scene)
        if (PlayerPrefs.HasKey("CurrentLevel"))
        {
            int level = PlayerPrefs.GetInt("CurrentLevel");
            return level;
        }
        
        // Fallback: trả về 1 nếu không tìm thấy
        Debug.LogWarning("QuestManager: Không tìm thấy CurrentLevel trong PlayerPrefs!");
        return 1;
    }
    
    /// <summary>
    /// Lưu số sao vào JSON và unlock level tiếp theo
    /// </summary>
    /// <param name="stars">Số sao đạt được (1-3)</param>
    void SaveStarsToLevelData(int stars)
    {
        int currentLevel = GetCurrentLevelFromScene();
        Debug.Log($"Current level: {currentLevel}");
        
        // Lưu kết quả sao vào JSON (tự động unlock quest tiếp theo)
        QuestDataStorage.SaveQuestStars(currentLevel, stars);
    }

    /// <summary>
    /// Tính số sao dựa trên thời gian hoàn thành quest
    /// </summary>
    /// <returns>Số sao đạt được (1-3)</returns>
    int CalculateStars()
    {
        if (currentQuest == null)
            return 1;

        float time = gameElapsedTime;

        // Nếu thời gian <= timeFor3Stars: 3 sao
        if (time <= currentQuest.timeFor3Stars)
        {
            return 3;
        }
        // Nếu thời gian <= timeFor2Stars: 2 sao
        else if (time <= currentQuest.timeFor2Stars)
        {
            return 2;
        }
        // Nếu thời gian > timeFor2Stars: 1 sao
        else
        {
            return 1;
        }
    }

    /// <summary>
    /// Lấy thời gian game đã trôi qua tính bằng giây
    /// </summary>
    public float GetGameTime()
    {
        return gameElapsedTime;
    }

    /// <summary>
    /// Lấy thời gian còn lại dưới dạng chuỗi định dạng (MM:SS) - Countdown
    /// </summary>
    public string GetGameTimeFormatted()
    {
        // Nếu có timeLimit, hiển thị thời gian còn lại (countdown)
        if (currentQuest != null && currentQuest.timeLimit > 0)
        {
            float remainingTime = currentQuest.timeLimit - gameElapsedTime;
            remainingTime = Mathf.Max(0f, remainingTime); // Đảm bảo không âm
            
            int minutes = Mathf.FloorToInt(remainingTime / 60f);
            int seconds = Mathf.FloorToInt(remainingTime % 60f);
            return string.Format("{0:00}:{1:00}", minutes, seconds);
        }
        else
        {
            // Nếu không có timeLimit, hiển thị thời gian đã trôi qua (tăng dần)
        int minutes = Mathf.FloorToInt(gameElapsedTime / 60f);
        int seconds = Mathf.FloorToInt(gameElapsedTime % 60f);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
        }
    }

    /// <summary>
    /// Reset thời gian game về 0
    /// </summary>
    public void ResetGameTime()
    {
        gameStartTime = Time.time;
        gameElapsedTime = 0f;
    }

    /// <summary>
    /// Lấy reward dựa trên số sao đạt được
    /// </summary>
    /// <param name="stars">Số sao (1-3)</param>
    /// <returns>Giá trị reward</returns>
    int GetRewardByStars(int stars)
    {
        if (currentQuest == null || currentQuest.rewardList == null)
            return 0;

        if (stars < 1 || stars > 3)
            return 0;

        // Index = stars - 1 (vì 1 sao -> index 0, 2 sao -> index 1, 3 sao -> index 2)
        if (stars - 1 < currentQuest.rewardList.Count)
        {
            return currentQuest.rewardList[stars - 1];
        }

        return 0;
    }

    /// <summary>
    /// Lưu reward vào PlayerData
    /// </summary>
    /// <param name="reward">Giá trị reward cần thêm</param>
    void SaveRewardToPlayerData(int reward)
    {
        if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.playerData != null)
        {
            PlayerDataManager.Instance.playerData.totalReward += reward;
            PlayerDataManager.Instance.Save();
            Debug.Log($"Đã nhận {reward} reward. Tổng reward: {PlayerDataManager.Instance.playerData.totalReward}");
        }
    }

}