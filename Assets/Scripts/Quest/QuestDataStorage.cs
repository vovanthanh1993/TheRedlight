using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Class để lưu trữ và load QuestData từ JSON
/// Chỉ sử dụng PersistentDataPath, tự động tạo file quest lần đầu chạy game
/// </summary>
public static class QuestDataStorage
{
    private const string QuestFileName = "quests.json";
    
    /// <summary>
    /// Đường dẫn đến file quests.json trong persistentDataPath
    /// </summary>
    private static string PersistentDataPath => Path.Combine(Application.persistentDataPath, QuestFileName);
    
    /// <summary>
    /// Lấy đường dẫn file quests.json (chỉ dùng PersistentDataPath)
    /// </summary>
    private static string QuestFilePath => PersistentDataPath;
    
    /// <summary>
    /// Public property để Editor script có thể truy cập
    /// </summary>
    public static string GetQuestFilePath() => QuestFilePath;
    
    /// <summary>
    /// Tạo file quest mặc định nếu chưa có (50 level với pattern lặp lại 15 level)
    /// </summary>
    private static void CreateDefaultQuestFileIfNotExists()
    {
        if (File.Exists(QuestFilePath))
        {
            return; // File đã tồn tại, không cần tạo
        }
        
        Debug.Log("QuestDataStorage: Không tìm thấy file quest, đang tạo file quest mặc định (50 level)...");
        
        // Tạo thư mục nếu chưa có
        string directory = Path.GetDirectoryName(QuestFilePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        // Tạo 50 level với pattern lặp lại 15 level
        Dictionary<int, QuestData> quests = new Dictionary<int, QuestData>();
        Dictionary<int, QuestData> patternQuests = new Dictionary<int, QuestData>();
        
        // Tham số pattern (giống như trong QuestDataGenerator)
        int patternLength = 15;
        int baseEnergyItems = 3;
        int energyItemsIncrement = 1;
        float baseTimeLimit = 120f;
        float timeLimitIncrement = 10f;
        int baseReward1Star = 50;
        int baseReward2Star = 100;
        int baseReward3Star = 150;
        int rewardIncrement = 25;
        
        // Tạo pattern cho 15 level đầu
        for (int patternLevel = 1; patternLevel <= patternLength; patternLevel++)
        {
            QuestData quest = ScriptableObject.CreateInstance<QuestData>();
            quest.questId = patternLevel;
            quest.objectives = new QuestObjective[0];
            
            // Tính số điểm EnergyItem (tăng dần từ baseEnergyItems, tối đa 20)
            int energyPoints = baseEnergyItems + (patternLevel - 1) * energyItemsIncrement;
            quest.requiredEnergyPoints = Mathf.Min(energyPoints, 20);
            
            // Tính Time Limit (tăng dần)
            quest.timeLimit = baseTimeLimit + (patternLevel - 1) * timeLimitIncrement;
            
            // Tính thời gian để đạt sao
            quest.timeFor3Stars = quest.timeLimit * 0.4f;
            quest.timeFor2Stars = quest.timeLimit * 0.7f;
            
            // Tính reward (tăng dần theo level)
            quest.rewardList = new List<int>
            {
                baseReward1Star + (patternLevel - 1) * rewardIncrement,
                baseReward2Star + (patternLevel - 1) * rewardIncrement,
                baseReward3Star + (patternLevel - 1) * rewardIncrement
            };
            
            patternQuests[patternLevel] = quest;
        }
        
        // Tạo tất cả 50 level bằng cách lặp lại pattern
        for (int level = 1; level <= 50; level++)
        {
            // Tính level trong pattern (1-15)
            int patternIndex = ((level - 1) % patternLength) + 1;
            QuestData patternQuest = patternQuests[patternIndex];
            
            // Tạo quest mới với ID là level hiện tại
            QuestData quest = ScriptableObject.CreateInstance<QuestData>();
            quest.questId = level;
            quest.objectives = patternQuest.objectives;
            quest.requiredEnergyPoints = patternQuest.requiredEnergyPoints;
            quest.timeLimit = patternQuest.timeLimit;
            quest.timeFor3Stars = patternQuest.timeFor3Stars;
            quest.timeFor2Stars = patternQuest.timeFor2Stars;
            
            // Reward vẫn tăng dần theo level thực tế
            quest.rewardList = new List<int>
            {
                baseReward1Star + (level - 1) * rewardIncrement,
                baseReward2Star + (level - 1) * rewardIncrement,
                baseReward3Star + (level - 1) * rewardIncrement
            };
            
            // Set locked status: chỉ level 1 unlock, các level khác locked
            quests[level] = quest;
        }
        
        // Lưu vào JSON
        SaveAllQuests(quests);
        
        Debug.Log($"QuestDataStorage: Đã tạo file quest mặc định với {quests.Count} level tại {QuestFilePath}");
    }
    
    /// <summary>
    /// Load tất cả quest từ JSON file
    /// Tự động tạo file quest mặc định nếu chưa có
    /// </summary>
    public static Dictionary<int, QuestData> LoadAllQuests()
    {
        // Tạo file quest mặc định nếu chưa có
        CreateDefaultQuestFileIfNotExists();
        
        Dictionary<int, QuestData> quests = new Dictionary<int, QuestData>();
        
        if (!File.Exists(QuestFilePath))
        {
            Debug.LogError($"QuestDataStorage: Không thể tạo file quest tại {QuestFilePath}!");
            return quests;
        }
        
        try
        {
            string json = File.ReadAllText(QuestFilePath);
            if (!string.IsNullOrEmpty(json))
            {
                QuestDataList questList = JsonUtility.FromJson<QuestDataList>(json);
                if (questList != null && questList.quests != null)
                {
                    foreach (var questJson in questList.quests)
                    {
                        QuestData questData = questJson.ToQuestData();
                        if (questData != null)
                        {
                            quests[questData.questId] = questData;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"QuestDataStorage: Lỗi khi load quest từ JSON: {ex.Message}");
        }
        
        return quests;
    }
    
    /// <summary>
    /// Load một quest cụ thể theo ID
    /// </summary>
    public static QuestData LoadQuest(int questId)
    {
        Dictionary<int, QuestData> allQuests = LoadAllQuests();
        if (allQuests.ContainsKey(questId))
        {
            return allQuests[questId];
        }
        
        Debug.LogWarning($"QuestDataStorage: Không tìm thấy quest với ID: {questId}");
        return null;
    }
    
    /// <summary>
    /// Lưu tất cả quest vào JSON file (chỉ lưu vào PersistentDataPath)
    /// </summary>
    public static void SaveAllQuests(Dictionary<int, QuestData> quests)
    {
        if (quests == null || quests.Count == 0)
        {
            Debug.LogWarning("QuestDataStorage: Không có quest nào để lưu!");
            return;
        }
        
        try
        {
            QuestDataList questList = new QuestDataList();
            questList.quests = new List<QuestDataJSON>();
            
            foreach (var quest in quests.Values)
            {
                QuestDataJSON questJson = new QuestDataJSON(quest);
                // Giữ nguyên stars và isLocked từ file cũ nếu có
                if (File.Exists(QuestFilePath))
                {
                    try
                    {
                        string oldJson = File.ReadAllText(QuestFilePath);
                        QuestDataList oldQuestList = JsonUtility.FromJson<QuestDataList>(oldJson);
                        if (oldQuestList != null && oldQuestList.quests != null)
                        {
                            foreach (var oldQuestJson in oldQuestList.quests)
                            {
                                if (oldQuestJson.questId == quest.questId)
                                {
                                    questJson.stars = oldQuestJson.stars;
                                    questJson.isLocked = oldQuestJson.isLocked;
                                    break;
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Nếu không đọc được file cũ, dùng giá trị mặc định
                    }
                }
                else
                {
                    // Lần đầu tạo file: chỉ level 1 unlock, các level khác locked
                    questJson.isLocked = quest.questId != 1;
                    questJson.stars = 0;
                }
                
                questList.quests.Add(questJson);
            }
            
            string json = JsonUtility.ToJson(questList, true);
            
            // Tạo thư mục nếu chưa có
            string directory = Path.GetDirectoryName(QuestFilePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            // Lưu vào PersistentDataPath
            File.WriteAllText(QuestFilePath, json);
            Debug.Log($"QuestDataStorage: Đã lưu {quests.Count} quest vào {QuestFilePath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"QuestDataStorage: Lỗi khi lưu quest vào JSON: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Lưu một quest cụ thể
    /// </summary>
    public static void SaveQuest(QuestData questData)
    {
        if (questData == null)
        {
            Debug.LogWarning("QuestDataStorage: QuestData là null!");
            return;
        }
        
        Dictionary<int, QuestData> allQuests = LoadAllQuests();
        allQuests[questData.questId] = questData;
        SaveAllQuests(allQuests);
    }
    
    /// <summary>
    /// Lưu kết quả sao cho một quest và unlock quest tiếp theo
    /// </summary>
    public static void SaveQuestStars(int questId, int stars)
    {
        // Đảm bảo file tồn tại
        CreateDefaultQuestFileIfNotExists();
        
        if (!File.Exists(QuestFilePath))
        {
            Debug.LogWarning($"QuestDataStorage: Không tìm thấy file JSON để lưu stars cho quest {questId}!");
            return;
        }
        
        try
        {
            string json = File.ReadAllText(QuestFilePath);
            QuestDataList questList = JsonUtility.FromJson<QuestDataList>(json);
            
            if (questList != null && questList.quests != null)
            {
                bool updated = false;
                
                foreach (var questJson in questList.quests)
                {
                    if (questJson.questId == questId)
                    {
                        // Chỉ cập nhật nếu số sao mới cao hơn
                        if (stars > questJson.stars)
                        {
                            questJson.stars = stars;
                            updated = true;
                            Debug.Log($"QuestDataStorage: Đã lưu {stars} sao cho quest {questId}");
                        }
                    }
                    
                    // Unlock quest tiếp theo nếu quest hiện tại đã hoàn thành
                    if (questJson.questId == questId + 1 && questJson.isLocked)
                    {
                        questJson.isLocked = false;
                        updated = true;
                        Debug.Log($"QuestDataStorage: Đã unlock quest {questId + 1}");
                    }
                }
                
                if (updated)
                {
                    // Lưu lại file
                    string updatedJson = JsonUtility.ToJson(questList, true);
                    File.WriteAllText(QuestFilePath, updatedJson);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"QuestDataStorage: Lỗi khi lưu stars: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Lấy kết quả sao của một quest
    /// </summary>
    public static int GetQuestStars(int questId)
    {
        // Đảm bảo file tồn tại
        CreateDefaultQuestFileIfNotExists();
        
        if (!File.Exists(QuestFilePath))
        {
            return 0;
        }
        
        try
        {
            string json = File.ReadAllText(QuestFilePath);
            QuestDataList questList = JsonUtility.FromJson<QuestDataList>(json);
            
            if (questList != null && questList.quests != null)
            {
                foreach (var questJson in questList.quests)
                {
                    if (questJson.questId == questId)
                    {
                        return questJson.stars;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"QuestDataStorage: Lỗi khi load stars từ JSON: {ex.Message}");
        }
        
        return 0;
    }
    
    /// <summary>
    /// Lấy trạng thái locked của một quest
    /// </summary>
    public static bool IsQuestLocked(int questId)
    {
        // Đảm bảo file tồn tại
        CreateDefaultQuestFileIfNotExists();
        
        if (!File.Exists(QuestFilePath))
        {
            // Quest đầu tiên không locked, các quest khác locked mặc định
            return questId != 1;
        }
        
        try
        {
            string json = File.ReadAllText(QuestFilePath);
            QuestDataList questList = JsonUtility.FromJson<QuestDataList>(json);
            
            if (questList != null && questList.quests != null)
            {
                foreach (var questJson in questList.quests)
                {
                    if (questJson.questId == questId)
                    {
                        return questJson.isLocked;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"QuestDataStorage: Lỗi khi load isLocked từ JSON: {ex.Message}");
        }
        
        // Fallback: Quest đầu tiên không locked, các quest khác locked
        return questId != 1;
    }
    
    /// <summary>
    /// Unlock một quest
    /// </summary>
    public static void UnlockQuest(int questId)
    {
        // Đảm bảo file tồn tại
        CreateDefaultQuestFileIfNotExists();
        
        if (!File.Exists(QuestFilePath))
        {
            Debug.LogWarning($"QuestDataStorage: Không tìm thấy file JSON để unlock quest {questId}!");
            return;
        }
        
        try
        {
            string json = File.ReadAllText(QuestFilePath);
            QuestDataList questList = JsonUtility.FromJson<QuestDataList>(json);
            
            if (questList != null && questList.quests != null)
            {
                foreach (var questJson in questList.quests)
                {
                    if (questJson.questId == questId)
                    {
                        if (questJson.isLocked)
                        {
                            questJson.isLocked = false;
                            Debug.Log($"QuestDataStorage: Đã unlock quest {questId}");
                            
                            // Lưu lại file
                            string updatedJson = JsonUtility.ToJson(questList, true);
                            File.WriteAllText(QuestFilePath, updatedJson);
                        }
                        return;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"QuestDataStorage: Lỗi khi unlock quest: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Unlock tất cả các quest (dùng cho cheat code F1)
    /// </summary>
    public static void UnlockAllQuests()
    {
        // Đảm bảo file tồn tại
        CreateDefaultQuestFileIfNotExists();
        
        if (!File.Exists(QuestFilePath))
        {
            Debug.LogWarning($"QuestDataStorage: Không tìm thấy file JSON để unlock tất cả quest tại {QuestFilePath}!");
            return;
        }
        
        try
        {
            string json = File.ReadAllText(QuestFilePath);
            QuestDataList questList = JsonUtility.FromJson<QuestDataList>(json);
            
            if (questList != null && questList.quests != null)
            {
                bool updated = false;
                int unlockedCount = 0;
                
                foreach (var questJson in questList.quests)
                {
                    if (questJson.isLocked)
                    {
                        questJson.isLocked = false;
                        updated = true;
                        unlockedCount++;
                    }
                }
                
                if (updated)
                {
                    string updatedJson = JsonUtility.ToJson(questList, true);
                    File.WriteAllText(QuestFilePath, updatedJson);
                    Debug.Log($"QuestDataStorage: Đã unlock {unlockedCount} quest!");
                }
                else
                {
                    Debug.Log("QuestDataStorage: Tất cả quest đã được unlock rồi!");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"QuestDataStorage: Lỗi khi unlock tất cả quest: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Reset tất cả quest về trạng thái ban đầu (chỉ level 1 unlock, các level khác locked, stars = 0)
    /// </summary>
    public static void ResetAllQuests()
    {
        // Đảm bảo file tồn tại
        CreateDefaultQuestFileIfNotExists();
        
        if (!File.Exists(QuestFilePath))
        {
            Debug.LogWarning($"QuestDataStorage: Không tìm thấy file JSON để reset quest tại {QuestFilePath}!");
            return;
        }
        
        try
        {
            string json = File.ReadAllText(QuestFilePath);
            QuestDataList questList = JsonUtility.FromJson<QuestDataList>(json);
            
            if (questList != null && questList.quests != null)
            {
                bool updated = false;
                int resetCount = 0;
                
                foreach (var questJson in questList.quests)
                {
                    bool needsUpdate = false;
                    
                    // Reset locked status: chỉ level 1 unlock, các level khác locked
                    if (questJson.questId == 1)
                    {
                        if (questJson.isLocked)
                        {
                            questJson.isLocked = false;
                            needsUpdate = true;
                        }
                    }
                    else
                    {
                        if (!questJson.isLocked)
                        {
                            questJson.isLocked = true;
                            needsUpdate = true;
                        }
                    }
                    
                    // Reset stars về 0
                    if (questJson.stars != 0)
                    {
                        questJson.stars = 0;
                        needsUpdate = true;
                    }
                    
                    if (needsUpdate)
                    {
                        updated = true;
                        resetCount++;
                    }
                }
                
                if (updated)
                {
                    string updatedJson = JsonUtility.ToJson(questList, true);
                    File.WriteAllText(QuestFilePath, updatedJson);
                    Debug.Log($"QuestDataStorage: Đã reset {resetCount} quest về trạng thái ban đầu!");
                }
                else
                {
                    Debug.Log("QuestDataStorage: Tất cả quest đã ở trạng thái ban đầu rồi!");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"QuestDataStorage: Lỗi khi reset tất cả quest: {ex.Message}");
        }
    }
}

/// <summary>
/// Class JSON serializable cho QuestData
/// </summary>
[Serializable]
public class QuestDataJSON
{
    public int questId;
    public QuestObjective[] objectives;
    public float timeFor3Stars;
    public float timeFor2Stars;
    public float timeLimit;
    public int requiredEnergyPoints;
    public int[] rewardList;
    public int stars = 0; // Kết quả sao đạt được (0 = chưa hoàn thành, 1-3 = số sao)
    public bool isLocked = true; // Trạng thái locked (true = bị khóa, false = đã unlock)
    
    public QuestDataJSON() { }
    
    public QuestDataJSON(QuestData questData)
    {
        if (questData == null) return;
        
        questId = questData.questId;
        objectives = questData.objectives;
        timeFor3Stars = questData.timeFor3Stars;
        timeFor2Stars = questData.timeFor2Stars;
        timeLimit = questData.timeLimit;
        requiredEnergyPoints = questData.requiredEnergyPoints;
        rewardList = questData.rewardList != null ? questData.rewardList.ToArray() : new int[] { 50, 100, 150 };
        stars = 0; // Mặc định chưa có sao
        isLocked = questId != 1; // Quest đầu tiên không locked, các quest khác locked mặc định
    }
    
    public QuestData ToQuestData()
    {
        QuestData questData = ScriptableObject.CreateInstance<QuestData>();
        questData.questId = questId;
        questData.objectives = objectives;
        questData.timeFor3Stars = timeFor3Stars;
        questData.timeFor2Stars = timeFor2Stars;
        questData.timeLimit = timeLimit;
        questData.requiredEnergyPoints = requiredEnergyPoints;
        questData.rewardList = rewardList != null ? new List<int>(rewardList) : new List<int> { 50, 100, 150 };
        return questData;
    }
}

/// <summary>
/// Wrapper class để serialize list quest
/// </summary>
[Serializable]
public class QuestDataList
{
    public List<QuestDataJSON> quests;
}

