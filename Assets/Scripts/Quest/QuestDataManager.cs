using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manager để quản lý QuestData đã được tạo sẵn trong JSON (QuestDataStorage)
/// Không còn tạo quest với objectives (collect trái cây) nữa
/// </summary>
public class QuestDataManager : MonoBehaviour
{
    public static QuestDataManager Instance { get; private set; }

    private Dictionary<int, QuestData> questsCache = new Dictionary<int, QuestData>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        LoadOrCreateQuests();
    }

    /// <summary>
    /// Load quest từ JSON (QuestDataStorage sẽ tự tạo file mặc định nếu chưa có)
    /// </summary>
    public void LoadOrCreateQuests()
    {
        questsCache = QuestDataStorage.LoadAllQuests();
        Debug.Log($"QuestDataManager: Đã load {questsCache.Count} quest");
    }

    /// <summary>
    /// Lấy quest theo ID từ cache
    /// </summary>
    public QuestData GetQuest(int questId)
    {
        if (questsCache.ContainsKey(questId))
        {
            return questsCache[questId];
        }
        
        // Nếu không có trong cache, thử load từ storage
        QuestData quest = QuestDataStorage.LoadQuest(questId);
        if (quest != null)
        {
            questsCache[questId] = quest;
        }
        
        return quest;
    }

    /// <summary>
    /// Lấy tất cả quest từ cache
    /// </summary>
    public Dictionary<int, QuestData> GetAllQuests()
    {
        return questsCache;
    }

    /// <summary>
    /// Lấy số lượng quest
    /// </summary>
    public int GetQuestCount()
    {
        return questsCache != null ? questsCache.Count : 0;
    }

    /// <summary>
    /// Lấy kết quả sao của một quest
    /// </summary>
    public int GetQuestStars(int questId)
    {
        return QuestDataStorage.GetQuestStars(questId);
    }

    /// <summary>
    /// Kiểm tra quest có bị locked không
    /// </summary>
    public bool IsQuestLocked(int questId)
    {
        return QuestDataStorage.IsQuestLocked(questId);
    }

    /// <summary>
    /// Refresh cache từ JSON
    /// </summary>
    public void Refresh()
    {
        questsCache = QuestDataStorage.LoadAllQuests();
        Debug.Log($"QuestDataManager: Đã refresh, có {questsCache.Count} quest");
    }
}

